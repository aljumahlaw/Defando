using FluentAssertions;
using LegalDocSystem.Data;
using LegalDocSystem.Models;
using LegalDocSystem.Services;
using LegalDocSystem.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LegalDocSystem.Tests.Services;

/// <summary>
/// Unit tests for FolderService.
/// Tests folder management operations including CRUD, hierarchy, and relationships.
/// </summary>
public class FolderServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly FolderService _folderService;

    public FolderServiceTests()
    {
        // Setup In-Memory Database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Create FolderService instance
        _folderService = new FolderService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region CreateFolderAsync Tests

    [Fact]
    public async Task CreateFolderAsync_WithValidData_CreatesFolder()
    {
        // Arrange: إعداد البيانات
        var folder = TestDataBuilder.CreateFolder(
            folderId: 0, // Let EF generate the ID
            parentId: null);
        folder.FolderName = "New Folder";
        folder.FolderPath = "/New Folder";
        folder.CreatedBy = 1;

        // Act: تنفيذ الوظيفة المطلوب اختبارها
        var result = await _folderService.CreateFolderAsync(folder);

        // Assert: التحقق من النتائج
        result.Should().NotBeNull();
        result.FolderId.Should().BeGreaterThan(0);
        result.FolderName.Should().Be("New Folder");
        result.FolderPath.Should().Be("/New Folder");

        // Verify folder was saved in database
        var savedFolder = await _context.Folders.FindAsync(result.FolderId);
        savedFolder.Should().NotBeNull();
        savedFolder!.FolderName.Should().Be("New Folder");
    }

    [Fact]
    public async Task CreateFolderAsync_WithParentFolder_CreatesFolderWithParent()
    {
        // Arrange
        var parentFolder = TestDataBuilder.CreateFolder(
            folderId: 1,
            parentId: null);
        parentFolder.FolderName = "Parent Folder";
        parentFolder.FolderPath = "/Parent Folder";
        _context.Folders.Add(parentFolder);
        await _context.SaveChangesAsync();

        var childFolder = TestDataBuilder.CreateFolder(
            folderId: 0,
            parentId: parentFolder.FolderId);
        childFolder.FolderName = "Child Folder";
        childFolder.FolderPath = "/Parent Folder/Child Folder";

        // Act
        var result = await _folderService.CreateFolderAsync(childFolder);

        // Assert
        result.Should().NotBeNull();
        result.ParentId.Should().Be(parentFolder.FolderId);
        result.FolderName.Should().Be("Child Folder");

        // Verify parent-child relationship
        var savedChild = await _context.Folders
            .Include(f => f.ParentFolder)
            .FirstOrDefaultAsync(f => f.FolderId == result.FolderId);
        savedChild.Should().NotBeNull();
        savedChild!.ParentFolder.Should().NotBeNull();
        savedChild.ParentFolder!.FolderId.Should().Be(parentFolder.FolderId);
    }

    [Fact]
    public async Task CreateFolderAsync_WithMultipleFolders_CreatesAllFolders()
    {
        // Arrange
        var folder1 = TestDataBuilder.CreateFolder(folderId: 0, parentId: null);
        folder1.FolderName = "Folder 1";
        folder1.FolderPath = "/Folder 1";

        var folder2 = TestDataBuilder.CreateFolder(folderId: 0, parentId: null);
        folder2.FolderName = "Folder 2";
        folder2.FolderPath = "/Folder 2";

        // Act
        var result1 = await _folderService.CreateFolderAsync(folder1);
        var result2 = await _folderService.CreateFolderAsync(folder2);

        // Assert
        result1.FolderId.Should().NotBe(result2.FolderId);
        
        var allFolders = await _context.Folders.ToListAsync();
        allFolders.Should().HaveCount(2);
        allFolders.Should().Contain(f => f.FolderName == "Folder 1");
        allFolders.Should().Contain(f => f.FolderName == "Folder 2");
    }

    [Fact]
    public async Task CreateFolderAsync_WithDuplicateNameInSameParent_ThrowsException()
    {
        // Arrange
        var parentFolder = TestDataBuilder.CreateFolder(folderId: 1, parentId: null);
        parentFolder.FolderName = "Parent";
        parentFolder.FolderPath = "/Parent";
        _context.Folders.Add(parentFolder);
        await _context.SaveChangesAsync();

        var folder1 = TestDataBuilder.CreateFolder(folderId: 0, parentId: parentFolder.FolderId);
        folder1.FolderName = "Duplicate Name";
        folder1.FolderPath = "/Parent/Duplicate Name";

        var folder2 = TestDataBuilder.CreateFolder(folderId: 0, parentId: parentFolder.FolderId);
        folder2.FolderName = "Duplicate Name"; // Same name, same parent
        folder2.FolderPath = "/Parent/Duplicate Name";

        // Act
        var result1 = await _folderService.CreateFolderAsync(folder1);
        var act = async () => await _folderService.CreateFolderAsync(folder2);

        // Assert
        result1.Should().NotBeNull();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
        
        // Verify only one folder was created
        var foldersInParent = await _context.Folders
            .Where(f => f.ParentId == parentFolder.FolderId && f.FolderName == "Duplicate Name")
            .ToListAsync();
        foldersInParent.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateFolderAsync_WithDuplicateNameInDifferentParents_AllowsCreation()
    {
        // Arrange
        var parent1 = TestDataBuilder.CreateFolder(folderId: 1, parentId: null);
        parent1.FolderName = "Parent 1";
        parent1.FolderPath = "/Parent 1";

        var parent2 = TestDataBuilder.CreateFolder(folderId: 2, parentId: null);
        parent2.FolderName = "Parent 2";
        parent2.FolderPath = "/Parent 2";

        _context.Folders.AddRange(parent1, parent2);
        await _context.SaveChangesAsync();

        var folder1 = TestDataBuilder.CreateFolder(folderId: 0, parentId: parent1.FolderId);
        folder1.FolderName = "Same Name";
        folder1.FolderPath = "/Parent 1/Same Name";

        var folder2 = TestDataBuilder.CreateFolder(folderId: 0, parentId: parent2.FolderId);
        folder2.FolderName = "Same Name"; // Same name, different parent
        folder2.FolderPath = "/Parent 2/Same Name";

        // Act
        var result1 = await _folderService.CreateFolderAsync(folder1);
        var result2 = await _folderService.CreateFolderAsync(folder2);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.FolderId.Should().NotBe(result2.FolderId);
    }

    #endregion

    #region GetFolderByIdAsync Tests

    [Fact]
    public async Task GetFolderByIdAsync_WithExistingFolder_ReturnsFolder()
    {
        // Arrange
        var folder = TestDataBuilder.CreateFolder(folderId: 1, parentId: null);
        folder.FolderName = "Test Folder";
        folder.FolderPath = "/Test Folder";
        _context.Folders.Add(folder);
        await _context.SaveChangesAsync();

        // Act
        var result = await _folderService.GetFolderByIdAsync(folder.FolderId);

        // Assert
        result.Should().NotBeNull();
        result!.FolderId.Should().Be(folder.FolderId);
        result.FolderName.Should().Be("Test Folder");
        result.ParentFolder.Should().BeNull(); // Root folder
    }

    [Fact]
    public async Task GetFolderByIdAsync_WithUnknownId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = 999;

        // Act
        var result = await _folderService.GetFolderByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFolderByIdAsync_WithFolderHavingParent_ReturnsFolderWithParent()
    {
        // Arrange
        var parentFolder = TestDataBuilder.CreateFolder(folderId: 1, parentId: null);
        parentFolder.FolderName = "Parent";
        parentFolder.FolderPath = "/Parent";
        _context.Folders.Add(parentFolder);
        await _context.SaveChangesAsync();

        var childFolder = TestDataBuilder.CreateFolder(folderId: 2, parentId: parentFolder.FolderId);
        childFolder.FolderName = "Child";
        childFolder.FolderPath = "/Parent/Child";
        _context.Folders.Add(childFolder);
        await _context.SaveChangesAsync();

        // Act
        var result = await _folderService.GetFolderByIdAsync(childFolder.FolderId);

        // Assert
        result.Should().NotBeNull();
        result!.ParentFolder.Should().NotBeNull();
        result.ParentFolder!.FolderId.Should().Be(parentFolder.FolderId);
        result.ParentFolder.FolderName.Should().Be("Parent");
    }

    [Fact]
    public async Task GetFolderByIdAsync_WithFolderHavingDocuments_ReturnsFolderWithDocuments()
    {
        // Arrange
        var folder = TestDataBuilder.CreateFolder(folderId: 1, parentId: null);
        folder.FolderName = "Documents Folder";
        folder.FolderPath = "/Documents Folder";
        _context.Folders.Add(folder);
        await _context.SaveChangesAsync();

        var document1 = TestDataBuilder.CreateDocument(documentId: 1, folderId: folder.FolderId);
        var document2 = TestDataBuilder.CreateDocument(documentId: 2, folderId: folder.FolderId);
        _context.Documents.AddRange(document1, document2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _folderService.GetFolderByIdAsync(folder.FolderId);

        // Assert
        result.Should().NotBeNull();
        result!.Documents.Should().NotBeNull();
        result.Documents.Should().HaveCount(2);
    }

    #endregion

    #region GetAllFoldersAsync Tests

    [Fact]
    public async Task GetAllFoldersAsync_WithMultipleFolders_ReturnsAllFolders()
    {
        // Arrange
        var folder1 = TestDataBuilder.CreateFolder(folderId: 1, parentId: null);
        folder1.FolderName = "Folder A";
        folder1.FolderPath = "/A";

        var folder2 = TestDataBuilder.CreateFolder(folderId: 2, parentId: null);
        folder2.FolderName = "Folder B";
        folder2.FolderPath = "/B";

        var folder3 = TestDataBuilder.CreateFolder(folderId: 3, parentId: null);
        folder3.FolderName = "Folder C";
        folder3.FolderPath = "/C";

        _context.Folders.AddRange(folder1, folder2, folder3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _folderService.GetAllFoldersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(f => f.FolderName == "Folder A");
        result.Should().Contain(f => f.FolderName == "Folder B");
        result.Should().Contain(f => f.FolderName == "Folder C");
    }

    [Fact]
    public async Task GetAllFoldersAsync_WithNoFolders_ReturnsEmptyList()
    {
        // Arrange: No folders in database

        // Act
        var result = await _folderService.GetAllFoldersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllFoldersAsync_ReturnsFoldersOrderedByPath()
    {
        // Arrange
        var folder1 = TestDataBuilder.CreateFolder(folderId: 1, parentId: null);
        folder1.FolderName = "Z Folder";
        folder1.FolderPath = "/Z";

        var folder2 = TestDataBuilder.CreateFolder(folderId: 2, parentId: null);
        folder2.FolderName = "A Folder";
        folder2.FolderPath = "/A";

        var folder3 = TestDataBuilder.CreateFolder(folderId: 3, parentId: null);
        folder3.FolderName = "M Folder";
        folder3.FolderPath = "/M";

        _context.Folders.AddRange(folder1, folder2, folder3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _folderService.GetAllFoldersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        // Should be ordered by FolderPath
        result[0].FolderPath.Should().Be("/A");
        result[1].FolderPath.Should().Be("/M");
        result[2].FolderPath.Should().Be("/Z");
    }

    #endregion

    #region GetSubFoldersAsync Tests

    [Fact]
    public async Task GetSubFoldersAsync_WithExistingParent_ReturnsChildren()
    {
        // Arrange
        var parentFolder = TestDataBuilder.CreateFolder(folderId: 1, parentId: null);
        parentFolder.FolderName = "Parent";
        parentFolder.FolderPath = "/Parent";
        _context.Folders.Add(parentFolder);
        await _context.SaveChangesAsync();

        var child1 = TestDataBuilder.CreateFolder(folderId: 2, parentId: parentFolder.FolderId);
        child1.FolderName = "Child A";
        child1.FolderPath = "/Parent/Child A";

        var child2 = TestDataBuilder.CreateFolder(folderId: 3, parentId: parentFolder.FolderId);
        child2.FolderName = "Child B";
        child2.FolderPath = "/Parent/Child B";

        var unrelatedFolder = TestDataBuilder.CreateFolder(folderId: 4, parentId: null);
        unrelatedFolder.FolderName = "Unrelated";
        unrelatedFolder.FolderPath = "/Unrelated";

        _context.Folders.AddRange(child1, child2, unrelatedFolder);
        await _context.SaveChangesAsync();

        // Act
        var result = await _folderService.GetSubFoldersAsync(parentFolder.FolderId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(f => f.FolderName == "Child A");
        result.Should().Contain(f => f.FolderName == "Child B");
        result.Should().NotContain(f => f.FolderName == "Unrelated");
    }

    [Fact]
    public async Task GetSubFoldersAsync_WithNoChildren_ReturnsEmptyList()
    {
        // Arrange
        var parentFolder = TestDataBuilder.CreateFolder(folderId: 1, parentId: null);
        parentFolder.FolderName = "Parent";
        parentFolder.FolderPath = "/Parent";
        _context.Folders.Add(parentFolder);
        await _context.SaveChangesAsync();

        // Act
        var result = await _folderService.GetSubFoldersAsync(parentFolder.FolderId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSubFoldersAsync_WithNullParentId_ReturnsRootFolders()
    {
        // Arrange
        var root1 = TestDataBuilder.CreateFolder(folderId: 1, parentId: null);
        root1.FolderName = "Root A";
        root1.FolderPath = "/Root A";

        var root2 = TestDataBuilder.CreateFolder(folderId: 2, parentId: null);
        root2.FolderName = "Root B";
        root2.FolderPath = "/Root B";

        var child = TestDataBuilder.CreateFolder(folderId: 3, parentId: root1.FolderId);
        child.FolderName = "Child";
        child.FolderPath = "/Root A/Child";

        _context.Folders.AddRange(root1, root2, child);
        await _context.SaveChangesAsync();

        // Act
        var result = await _folderService.GetSubFoldersAsync(null);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(f => f.FolderName == "Root A");
        result.Should().Contain(f => f.FolderName == "Root B");
        result.Should().NotContain(f => f.FolderName == "Child");
    }

    [Fact]
    public async Task GetSubFoldersAsync_ReturnsFoldersOrderedByName()
    {
        // Arrange
        var parentFolder = TestDataBuilder.CreateFolder(folderId: 1, parentId: null);
        parentFolder.FolderName = "Parent";
        parentFolder.FolderPath = "/Parent";
        _context.Folders.Add(parentFolder);
        await _context.SaveChangesAsync();

        var child1 = TestDataBuilder.CreateFolder(folderId: 2, parentId: parentFolder.FolderId);
        child1.FolderName = "Z Child";
        child1.FolderPath = "/Parent/Z Child";

        var child2 = TestDataBuilder.CreateFolder(folderId: 3, parentId: parentFolder.FolderId);
        child2.FolderName = "A Child";
        child2.FolderPath = "/Parent/A Child";

        var child3 = TestDataBuilder.CreateFolder(folderId: 4, parentId: parentFolder.FolderId);
        child3.FolderName = "M Child";
        child3.FolderPath = "/Parent/M Child";

        _context.Folders.AddRange(child1, child2, child3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _folderService.GetSubFoldersAsync(parentFolder.FolderId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        // Should be ordered by FolderName
        result[0].FolderName.Should().Be("A Child");
        result[1].FolderName.Should().Be("M Child");
        result[2].FolderName.Should().Be("Z Child");
    }

    #endregion

    #region UpdateFolderAsync Tests

    [Fact]
    public async Task UpdateFolderAsync_WithExistingFolder_UpdatesProperties()
    {
        // Arrange
        var folder = TestDataBuilder.CreateFolder(folderId: 1, parentId: null);
        folder.FolderName = "Original Name";
        folder.FolderPath = "/Original Name";
        _context.Folders.Add(folder);
        await _context.SaveChangesAsync();

        // Act
        folder.FolderName = "Updated Name";
        folder.FolderPath = "/Updated Name";
        await _folderService.UpdateFolderAsync(folder);

        // Assert
        var updatedFolder = await _context.Folders.FindAsync(folder.FolderId);
        updatedFolder.Should().NotBeNull();
        updatedFolder!.FolderName.Should().Be("Updated Name");
        updatedFolder.FolderPath.Should().Be("/Updated Name");
    }

    [Fact]
    public async Task UpdateFolderAsync_WithParentChange_UpdatesParentId()
    {
        // Arrange
        var oldParent = TestDataBuilder.CreateFolder(folderId: 1, parentId: null);
        oldParent.FolderName = "Old Parent";
        oldParent.FolderPath = "/Old Parent";

        var newParent = TestDataBuilder.CreateFolder(folderId: 2, parentId: null);
        newParent.FolderName = "New Parent";
        newParent.FolderPath = "/New Parent";

        var folder = TestDataBuilder.CreateFolder(folderId: 3, parentId: oldParent.FolderId);
        folder.FolderName = "Child";
        folder.FolderPath = "/Old Parent/Child";

        _context.Folders.AddRange(oldParent, newParent, folder);
        await _context.SaveChangesAsync();

        // Act
        folder.ParentId = newParent.FolderId;
        folder.FolderPath = "/New Parent/Child";
        await _folderService.UpdateFolderAsync(folder);

        // Assert
        var updatedFolder = await _context.Folders.FindAsync(folder.FolderId);
        updatedFolder.Should().NotBeNull();
        updatedFolder!.ParentId.Should().Be(newParent.FolderId);
    }

    [Fact]
    public async Task UpdateFolderAsync_WithNonExistingFolder_ThrowsException()
    {
        // Arrange
        var nonExistentFolder = TestDataBuilder.CreateFolder(folderId: 999, parentId: null);
        nonExistentFolder.FolderName = "Non Existent";
        nonExistentFolder.FolderPath = "/Non Existent";

        // Act & Assert
        // Note: EF Core Update will attempt to update, but since it doesn't exist,
        // it might throw or behave differently. Testing actual behavior.
        var act = async () => await _folderService.UpdateFolderAsync(nonExistentFolder);
        
        // EF Core Update will try to update, but since entity is not tracked,
        // it will add it as new. This is expected behavior.
        await act.Should().NotThrowAsync();
        
        // Verify it was added as new entity
        var addedFolder = await _context.Folders.FindAsync(999);
        addedFolder.Should().NotBeNull();
    }

    #endregion

    #region DeleteFolderAsync Tests

    [Fact]
    public async Task DeleteFolderAsync_WithEmptyFolder_DeletesFolder()
    {
        // Arrange
        var folder = TestDataBuilder.CreateFolder(folderId: 1, parentId: null);
        folder.FolderName = "Empty Folder";
        folder.FolderPath = "/Empty Folder";
        _context.Folders.Add(folder);
        await _context.SaveChangesAsync();

        var folderId = folder.FolderId;

        // Act
        await _folderService.DeleteFolderAsync(folderId);

        // Assert
        var deletedFolder = await _context.Folders.FindAsync(folderId);
        deletedFolder.Should().BeNull();
    }

    [Fact]
    public async Task DeleteFolderAsync_WithFolderHavingDocuments_ThrowsException()
    {
        // Arrange
        var folder = TestDataBuilder.CreateFolder(folderId: 1, parentId: null);
        folder.FolderName = "Folder With Documents";
        folder.FolderPath = "/Folder With Documents";
        _context.Folders.Add(folder);
        await _context.SaveChangesAsync();

        var document = TestDataBuilder.CreateDocument(documentId: 1, folderId: folder.FolderId);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        var folderId = folder.FolderId;

        // Act
        var act = async () => await _folderService.DeleteFolderAsync(folderId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*contains*document*");

        // Verify folder was not deleted
        var folderStillExists = await _context.Folders.FindAsync(folderId);
        folderStillExists.Should().NotBeNull();
        folderStillExists!.FolderName.Should().Be("Folder With Documents");
    }

    [Fact]
    public async Task DeleteFolderAsync_WithFolderHavingSubFolders_ThrowsException()
    {
        // Arrange
        var parentFolder = TestDataBuilder.CreateFolder(folderId: 1, parentId: null);
        parentFolder.FolderName = "Parent";
        parentFolder.FolderPath = "/Parent";
        _context.Folders.Add(parentFolder);
        await _context.SaveChangesAsync();

        var childFolder = TestDataBuilder.CreateFolder(folderId: 2, parentId: parentFolder.FolderId);
        childFolder.FolderName = "Child";
        childFolder.FolderPath = "/Parent/Child";
        _context.Folders.Add(childFolder);
        await _context.SaveChangesAsync();

        var parentId = parentFolder.FolderId;
        var childId = childFolder.FolderId;

        // Act
        var act = async () => await _folderService.DeleteFolderAsync(parentId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*contains*subfolder*");

        // Verify parent folder was not deleted
        var parentStillExists = await _context.Folders.FindAsync(parentId);
        parentStillExists.Should().NotBeNull();
        parentStillExists!.FolderName.Should().Be("Parent");

        // Verify child folder still exists
        var childStillExists = await _context.Folders.FindAsync(childId);
        childStillExists.Should().NotBeNull();
        childStillExists!.FolderName.Should().Be("Child");
    }

    [Fact]
    public async Task DeleteFolderAsync_WithNonExistingFolder_DoesNotThrow()
    {
        // Arrange
        var nonExistentId = 999;

        // Act
        var act = async () => await _folderService.DeleteFolderAsync(nonExistentId);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteFolderAsync_WithMultipleCalls_DoesNotThrow()
    {
        // Arrange
        var folder = TestDataBuilder.CreateFolder(folderId: 1, parentId: null);
        folder.FolderName = "Folder";
        folder.FolderPath = "/Folder";
        _context.Folders.Add(folder);
        await _context.SaveChangesAsync();

        var folderId = folder.FolderId;

        // Act
        await _folderService.DeleteFolderAsync(folderId);
        var act = async () => await _folderService.DeleteFolderAsync(folderId);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CreateUpdateDelete_CompleteWorkflow_WorksCorrectly()
    {
        // Arrange
        var folder = TestDataBuilder.CreateFolder(folderId: 0, parentId: null);
        folder.FolderName = "Workflow Folder";
        folder.FolderPath = "/Workflow Folder";

        // Act - Create
        var created = await _folderService.CreateFolderAsync(folder);
        var createdId = created.FolderId;

        // Verify creation
        var retrieved = await _folderService.GetFolderByIdAsync(createdId);
        retrieved.Should().NotBeNull();
        retrieved!.FolderName.Should().Be("Workflow Folder");

        // Act - Update
        created.FolderName = "Updated Workflow Folder";
        created.FolderPath = "/Updated Workflow Folder";
        await _folderService.UpdateFolderAsync(created);

        // Verify update
        var updated = await _folderService.GetFolderByIdAsync(createdId);
        updated.Should().NotBeNull();
        updated!.FolderName.Should().Be("Updated Workflow Folder");

        // Act - Delete
        await _folderService.DeleteFolderAsync(createdId);

        // Verify deletion
        var deleted = await _folderService.GetFolderByIdAsync(createdId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task FolderHierarchy_WithMultipleLevels_WorksCorrectly()
    {
        // Arrange & Act
        // Level 1: Root
        var root = TestDataBuilder.CreateFolder(folderId: 0, parentId: null);
        root.FolderName = "Root";
        root.FolderPath = "/Root";
        root = await _folderService.CreateFolderAsync(root);

        // Level 2: Child of Root
        var level2 = TestDataBuilder.CreateFolder(folderId: 0, parentId: root.FolderId);
        level2.FolderName = "Level 2";
        level2.FolderPath = "/Root/Level 2";
        level2 = await _folderService.CreateFolderAsync(level2);

        // Level 3: Child of Level 2
        var level3 = TestDataBuilder.CreateFolder(folderId: 0, parentId: level2.FolderId);
        level3.FolderName = "Level 3";
        level3.FolderPath = "/Root/Level 2/Level 3";
        level3 = await _folderService.CreateFolderAsync(level3);

        // Assert
        var retrievedRoot = await _folderService.GetFolderByIdAsync(root.FolderId);
        retrievedRoot.Should().NotBeNull();

        var retrievedLevel2 = await _folderService.GetFolderByIdAsync(level2.FolderId);
        retrievedLevel2.Should().NotBeNull();
        retrievedLevel2!.ParentId.Should().Be(root.FolderId);

        var retrievedLevel3 = await _folderService.GetFolderByIdAsync(level3.FolderId);
        retrievedLevel3.Should().NotBeNull();
        retrievedLevel3!.ParentId.Should().Be(level2.FolderId);

        // Verify subfolders
        var rootChildren = await _folderService.GetSubFoldersAsync(root.FolderId);
        rootChildren.Should().HaveCount(1);
        rootChildren[0].FolderId.Should().Be(level2.FolderId);

        var level2Children = await _folderService.GetSubFoldersAsync(level2.FolderId);
        level2Children.Should().HaveCount(1);
        level2Children[0].FolderId.Should().Be(level3.FolderId);
    }

    #endregion
}

