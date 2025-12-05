using FluentAssertions;
using Defando.Data;
using Defando.Models;
using Defando.Services;
using Defando.Tests.Helpers;
using Defando.ViewModels;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Defando.Tests.Services;

/// <summary>
/// Unit tests for DocumentService.
/// Tests document management, search, version control, and file operations.
/// </summary>
public class DocumentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IOcrService> _mockOcrService;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly DocumentService _documentService;

    public DocumentServiceTests()
    {
        // Setup In-Memory Database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Setup Mocks
        _mockOcrService = new Mock<IOcrService>();
        _mockFileStorageService = new Mock<IFileStorageService>();
        _mockAuditService = new Mock<IAuditService>();

        // Setup OcrService Mock
        _mockOcrService
            .Setup(x => x.IsOcrEnabled())
            .Returns(true);

        // Setup FileStorageService Mock
        // Note: IsExtensionAllowed is not in IFileStorageService interface but exists in FileStorageService
        // We'll mock it using reflection or create a wrapper, but for now we'll use a workaround
        // by checking the extension in the mock setup

        _mockFileStorageService
            .Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync((Stream stream, string fileName) => $"/storage/{Guid.NewGuid()}/{fileName}");

        _mockFileStorageService
            .Setup(x => x.GetFileSizeAsync(It.IsAny<string>()))
            .ReturnsAsync((string path) => 1024L);

        _mockFileStorageService
            .Setup(x => x.DeleteFileAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Setup AuditService Mock
        _mockAuditService
            .Setup(x => x.LogCreateAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockAuditService
            .Setup(x => x.LogUpdateAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockAuditService
            .Setup(x => x.LogDeleteAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Create DocumentService instance
        _documentService = new DocumentService(
            _context,
            _mockOcrService.Object,
            _mockFileStorageService.Object,
            _mockAuditService.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetAllDocumentsAsync Tests

    [Fact]
    public async Task GetAllDocumentsAsync_WithDocuments_ReturnsAllDocuments()
    {
        // Arrange: إعداد البيانات
        var user = TestDataBuilder.CreateUser(userId: 1, username: "uploader");
        var folder = TestDataBuilder.CreateFolder(folderId: 1);
        var document1 = TestDataBuilder.CreateDocument(documentId: 1, folderId: 1, uploadedBy: 1);
        var document2 = TestDataBuilder.CreateDocument(documentId: 2, folderId: 1, uploadedBy: 1);

        _context.Users.Add(user);
        _context.Folders.Add(folder);
        _context.Documents.AddRange(document1, document2);
        await _context.SaveChangesAsync();

        // Act: تنفيذ الوظيفة المطلوب اختبارها
        var result = await _documentService.GetAllDocumentsAsync();

        // Assert: التحقق من النتائج
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(d => d.DocumentId == 1);
        result.Should().Contain(d => d.DocumentId == 2);
    }

    [Fact]
    public async Task GetAllDocumentsAsync_WithNoDocuments_ReturnsEmptyList()
    {
        // Arrange: لا توجد مستندات في قاعدة البيانات

        // Act
        var result = await _documentService.GetAllDocumentsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllDocumentsAsync_OrdersByUploadedAtDescending()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document1 = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document1.UploadedAt = DateTime.UtcNow.AddDays(-2);

        var document2 = TestDataBuilder.CreateDocument(documentId: 2, uploadedBy: 1);
        document2.UploadedAt = DateTime.UtcNow.AddDays(-1);

        var document3 = TestDataBuilder.CreateDocument(documentId: 3, uploadedBy: 1);
        document3.UploadedAt = DateTime.UtcNow;

        _context.Users.Add(user);
        _context.Documents.AddRange(document1, document2, document3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.GetAllDocumentsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result[0].DocumentId.Should().Be(3); // Newest first
        result[1].DocumentId.Should().Be(2);
        result[2].DocumentId.Should().Be(1); // Oldest last
    }

    [Fact]
    public async Task GetAllDocumentsAsync_IncludesRelatedEntities()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1, username: "uploader");
        var folder = TestDataBuilder.CreateFolder(folderId: 1, folderName: "Test Folder");
        var document = TestDataBuilder.CreateDocument(documentId: 1, folderId: 1, uploadedBy: 1);

        _context.Users.Add(user);
        _context.Folders.Add(folder);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.GetAllDocumentsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Folder.Should().NotBeNull();
        result[0].UploadedByUser.Should().NotBeNull();
        result[0].Folder!.FolderName.Should().Be("Test Folder");
        result[0].UploadedByUser!.Username.Should().Be("uploader");
    }

    #endregion

    #region GetDocumentByIdAsync Tests

    [Fact]
    public async Task GetDocumentByIdAsync_WithExistingDocument_ReturnsDocument()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document.DocumentName = "Test Document";

        _context.Users.Add(user);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.GetDocumentByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.DocumentId.Should().Be(1);
        result.DocumentName.Should().Be("Test Document");
    }

    [Fact]
    public async Task GetDocumentByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange: لا توجد مستندات في قاعدة البيانات

        // Act
        var result = await _documentService.GetDocumentByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDocumentByIdAsync_IncludesVersions()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        var version1 = new DocumentVersion
        {
            VersionId = 1,
            DocumentId = 1,
            VersionNumber = 1,
            CreatedBy = 1,
            CreatedAt = DateTime.UtcNow
        };
        var version2 = new DocumentVersion
        {
            VersionId = 2,
            DocumentId = 1,
            VersionNumber = 2,
            CreatedBy = 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.Documents.Add(document);
        _context.DocumentVersions.AddRange(version1, version2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.GetDocumentByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Versions.Should().NotBeNull();
        result.Versions.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDocumentByIdAsync_IncludesLockedByUser()
    {
        // Arrange
        var user1 = TestDataBuilder.CreateUser(userId: 1, username: "uploader");
        var user2 = TestDataBuilder.CreateUser(userId: 2, username: "locker");
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document.IsLocked = true;
        document.LockedBy = 2;

        _context.Users.AddRange(user1, user2);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.GetDocumentByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.LockedByUser.Should().NotBeNull();
        result.LockedByUser!.Username.Should().Be("locker");
    }

    #endregion

    #region CreateDocumentAsync Tests

    [Fact]
    public async Task CreateDocumentAsync_WithValidData_CreatesDocument()
    {
        // Arrange
        var newDocument = TestDataBuilder.CreateDocument(
            documentId: 0, // Will be set by database
            folderId: null,
            uploadedBy: 1);
        newDocument.DocumentId = 0; // Reset to let database generate
        newDocument.DocumentName = "New Document";
        newDocument.DocumentType = "contract";

        var user = TestDataBuilder.CreateUser(userId: 1);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.CreateDocumentAsync(newDocument);

        // Assert
        result.Should().NotBeNull();
        result.DocumentId.Should().BeGreaterThan(0);
        result.DocumentName.Should().Be("New Document");
        result.DocumentType.Should().Be("contract");

        // Verify document was saved to database
        var savedDocument = await _context.Documents.FindAsync(result.DocumentId);
        savedDocument.Should().NotBeNull();
        savedDocument!.DocumentName.Should().Be("New Document");

        // Verify Audit Log was called
        _mockAuditService.Verify(
            x => x.LogCreateAsync(
                "Document",
                result.DocumentId,
                It.Is<string>(s => s.Contains("New Document"))),
            Times.Once);
    }

    [Fact]
    public async Task CreateDocumentAsync_WithFolderId_AssociatesWithFolder()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var folder = TestDataBuilder.CreateFolder(folderId: 1);
        var document = TestDataBuilder.CreateDocument(
            documentId: 0,
            folderId: 1,
            uploadedBy: 1);
        document.DocumentId = 0;

        _context.Users.Add(user);
        _context.Folders.Add(folder);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.CreateDocumentAsync(document);

        // Assert
        result.Should().NotBeNull();
        result.FolderId.Should().Be(1);

        var savedDocument = await _context.Documents
            .Include(d => d.Folder)
            .FirstOrDefaultAsync(d => d.DocumentId == result.DocumentId);
        savedDocument!.Folder.Should().NotBeNull();
        savedDocument.Folder!.FolderId.Should().Be(1);
    }

    [Fact]
    public async Task CreateDocumentAsync_WithFileGuid_KeepsFileGuid()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var fileGuid = Guid.NewGuid();
        var document = TestDataBuilder.CreateDocument(
            documentId: 0,
            uploadedBy: 1);
        document.DocumentId = 0;
        document.FileGuid = fileGuid;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.CreateDocumentAsync(document);

        // Assert
        result.FileGuid.Should().Be(fileGuid);
    }

    #endregion

    #region UpdateDocumentAsync Tests

    [Fact]
    public async Task UpdateDocumentAsync_WithValidData_UpdatesDocument()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document.DocumentName = "Original Name";
        document.DocumentType = "memo";

        _context.Users.Add(user);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Modify document
        document.DocumentName = "Updated Name";
        document.DocumentType = "contract";

        // Act
        await _documentService.UpdateDocumentAsync(document);

        // Assert
        var updatedDocument = await _context.Documents.FindAsync(1);
        updatedDocument.Should().NotBeNull();
        updatedDocument!.DocumentName.Should().Be("Updated Name");
        updatedDocument.DocumentType.Should().Be("contract");

        // Verify Audit Log was called
        _mockAuditService.Verify(
            x => x.LogUpdateAsync(
                "Document",
                1,
                It.Is<string>(s => s.Contains("Updated Name") || s.Contains("Updated fields"))),
            Times.Once);
    }

    [Fact]
    public async Task UpdateDocumentAsync_WithLockedDocument_StillUpdates()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document.IsLocked = true;
        document.LockedBy = 1;

        _context.Users.Add(user);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Modify document
        document.DocumentName = "Updated While Locked";

        // Act
        await _documentService.UpdateDocumentAsync(document);

        // Assert
        var updatedDocument = await _context.Documents.FindAsync(1);
        updatedDocument!.DocumentName.Should().Be("Updated While Locked");
    }

    [Fact]
    public async Task UpdateDocumentAsync_LogsFieldChanges()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document.DocumentName = "Original";
        document.DocumentType = "memo";

        _context.Users.Add(user);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Modify document
        document.DocumentName = "Updated";
        document.DocumentType = "contract";

        // Act
        await _documentService.UpdateDocumentAsync(document);

        // Assert
        _mockAuditService.Verify(
            x => x.LogUpdateAsync(
                "Document",
                1,
                It.Is<string>(s => 
                    s.Contains("Name:") && 
                    s.Contains("Type:"))),
            Times.Once);
    }

    #endregion

    #region DeleteDocumentAsync Tests

    [Fact]
    public async Task DeleteDocumentAsync_WithExistingDocument_DeletesDocument()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document.FilePath = "/storage/test/document.pdf";

        _context.Users.Add(user);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        await _documentService.DeleteDocumentAsync(1);

        // Assert
        var deletedDocument = await _context.Documents.FindAsync(1);
        deletedDocument.Should().BeNull();

        // Verify file deletion was attempted
        _mockFileStorageService.Verify(
            x => x.DeleteFileAsync("/storage/test/document.pdf"),
            Times.Once);

        // Verify Audit Log was called
        _mockAuditService.Verify(
            x => x.LogDeleteAsync(
                "Document",
                1,
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteDocumentAsync_WithNonExistentId_DoesNotThrow()
    {
        // Arrange: لا توجد مستندات في قاعدة البيانات

        // Act & Assert
        var act = async () => await _documentService.DeleteDocumentAsync(999);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteDocumentAsync_WithNoFilePath_DoesNotCallFileStorage()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document.FilePath = string.Empty;

        _context.Users.Add(user);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        await _documentService.DeleteDocumentAsync(1);

        // Assert
        var deletedDocument = await _context.Documents.FindAsync(1);
        deletedDocument.Should().BeNull();

        // Verify file deletion was NOT called
        _mockFileStorageService.Verify(
            x => x.DeleteFileAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteDocumentAsync_WithFileStorageError_StillDeletesFromDatabase()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document.FilePath = "/storage/test/document.pdf";

        _context.Users.Add(user);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Setup FileStorageService to throw exception
        _mockFileStorageService
            .Setup(x => x.DeleteFileAsync(It.IsAny<string>()))
            .ThrowsAsync(new IOException("File not found"));

        // Act
        await _documentService.DeleteDocumentAsync(1);

        // Assert
        var deletedDocument = await _context.Documents.FindAsync(1);
        deletedDocument.Should().BeNull(); // Still deleted from database

        // Verify error was logged
        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e => 
                e.Action == "delete_document_file_error")),
            Times.Once);
    }

    #endregion

    #region SearchDocumentsAsync Tests

    [Fact]
    public async Task SearchDocumentsAsync_WithMatchingQuery_ReturnsMatchingDocuments()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document1 = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document1.DocumentName = "Contract Agreement";

        var document2 = TestDataBuilder.CreateDocument(documentId: 2, uploadedBy: 1);
        document2.DocumentName = "Legal Memo";

        var document3 = TestDataBuilder.CreateDocument(documentId: 3, uploadedBy: 1);
        document3.DocumentName = "Contract Amendment";

        _context.Users.Add(user);
        _context.Documents.AddRange(document1, document2, document3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.SearchDocumentsAsync("Contract");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(d => d.DocumentName.Contains("Contract"));
        result.Should().NotContain(d => d.DocumentName == "Legal Memo");
    }

    [Fact]
    public async Task SearchDocumentsAsync_WithEmptyQuery_ReturnsEmptyList()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);

        _context.Users.Add(user);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.SearchDocumentsAsync("");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchDocumentsAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document.DocumentName = "Test Document";

        _context.Users.Add(user);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.SearchDocumentsAsync("NonExistentTerm");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchDocumentsAsync_SearchesInDocumentType()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document1 = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document1.DocumentName = "Document 1";
        document1.DocumentType = "contract";

        var document2 = TestDataBuilder.CreateDocument(documentId: 2, uploadedBy: 1);
        document2.DocumentName = "Document 2";
        document2.DocumentType = "memo";

        _context.Users.Add(user);
        _context.Documents.AddRange(document1, document2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.SearchDocumentsAsync("contract");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].DocumentType.Should().Be("contract");
    }

    #endregion

    #region AdvancedSearchAsync Tests

    [Fact]
    public async Task AdvancedSearchAsync_WithFolderFilter_ReturnsFilteredResults()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var folder1 = TestDataBuilder.CreateFolder(folderId: 1);
        var folder2 = TestDataBuilder.CreateFolder(folderId: 2);
        var document1 = TestDataBuilder.CreateDocument(documentId: 1, folderId: 1, uploadedBy: 1);
        var document2 = TestDataBuilder.CreateDocument(documentId: 2, folderId: 2, uploadedBy: 1);

        _context.Users.Add(user);
        _context.Folders.AddRange(folder1, folder2);
        _context.Documents.AddRange(document1, document2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.AdvancedSearchAsync(
            query: "",
            folderId: 1);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(1);
        result.Results[0].Document.FolderId.Should().Be(1);
    }

    [Fact]
    public async Task AdvancedSearchAsync_WithDocumentTypeFilter_ReturnsFilteredResults()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document1 = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document1.DocumentType = "contract";

        var document2 = TestDataBuilder.CreateDocument(documentId: 2, uploadedBy: 1);
        document2.DocumentType = "memo";

        _context.Users.Add(user);
        _context.Documents.AddRange(document1, document2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.AdvancedSearchAsync(
            query: "",
            documentType: "contract");

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(1);
        result.Results[0].Document.DocumentType.Should().Be("contract");
    }

    [Fact]
    public async Task AdvancedSearchAsync_WithDateRangeFilter_ReturnsFilteredResults()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document1 = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document1.UploadedAt = DateTime.UtcNow.AddDays(-5);

        var document2 = TestDataBuilder.CreateDocument(documentId: 2, uploadedBy: 1);
        document2.UploadedAt = DateTime.UtcNow.AddDays(-1);

        var document3 = TestDataBuilder.CreateDocument(documentId: 3, uploadedBy: 1);
        document3.UploadedAt = DateTime.UtcNow;

        _context.Users.Add(user);
        _context.Documents.AddRange(document1, document2, document3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.AdvancedSearchAsync(
            query: "",
            startDate: DateTime.UtcNow.AddDays(-2),
            endDate: DateTime.UtcNow);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(2); // document2 and document3
        result.Results.Should().NotContain(r => r.Document.DocumentId == 1);
    }

    [Fact]
    public async Task AdvancedSearchAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        for (int i = 1; i <= 15; i++)
        {
            var document = TestDataBuilder.CreateDocument(documentId: i, uploadedBy: 1);
            document.DocumentName = $"Document {i}";
            _context.Documents.Add(document);
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.AdvancedSearchAsync(
            query: "",
            page: 2,
            pageSize: 5);

        // Assert
        result.Should().NotBeNull();
        result.CurrentPage.Should().Be(2);
        result.PageSize.Should().Be(5);
        result.TotalCount.Should().Be(15);
        result.Results.Should().HaveCount(5);
    }

    [Fact]
    public async Task AdvancedSearchAsync_WithRelevanceSorting_SortsByRank()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document1 = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document1.DocumentName = "Test Document";

        var document2 = TestDataBuilder.CreateDocument(documentId: 2, uploadedBy: 1);
        document2.DocumentName = "Test Test Document";

        _context.Users.Add(user);
        _context.Documents.AddRange(document1, document2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.AdvancedSearchAsync(
            query: "Test",
            sortBy: "relevance");

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(2);
        // document2 should have higher rank (contains "Test" twice)
        result.Results[0].Rank.Should().BeGreaterOrEqualTo(result.Results[1].Rank);
    }

    #endregion

    #region CheckOutDocumentAsync Tests

    [Fact]
    public async Task CheckOutDocumentAsync_WithUnlockedDocument_LocksDocument()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document.IsLocked = false;

        _context.Users.Add(user);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        await _documentService.CheckOutDocumentAsync(1, 1);

        // Assert
        var updatedDocument = await _context.Documents.FindAsync(1);
        updatedDocument.Should().NotBeNull();
        updatedDocument!.IsLocked.Should().BeTrue();
        updatedDocument.LockedBy.Should().Be(1);
        updatedDocument.LockedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CheckOutDocumentAsync_WithAlreadyLockedDocument_ThrowsException()
    {
        // Arrange
        var user1 = TestDataBuilder.CreateUser(userId: 1);
        var user2 = TestDataBuilder.CreateUser(userId: 2);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document.IsLocked = true;
        document.LockedBy = 2;

        _context.Users.AddRange(user1, user2);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act & Assert
        var act = async () => await _documentService.CheckOutDocumentAsync(1, 1);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already locked*");
    }

    [Fact]
    public async Task CheckOutDocumentAsync_WithSameUserAlreadyLocked_ThrowsException()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document.IsLocked = true;
        document.LockedBy = 1;

        _context.Users.Add(user);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act & Assert
        var act = async () => await _documentService.CheckOutDocumentAsync(1, 1);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already have this document checked out*");
    }

    [Fact]
    public async Task CheckOutDocumentAsync_WithNonExistentDocument_ThrowsException()
    {
        // Arrange: لا توجد مستندات في قاعدة البيانات

        // Act & Assert
        var act = async () => await _documentService.CheckOutDocumentAsync(999, 1);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region CheckInDocumentAsync Tests

    [Fact]
    public async Task CheckInDocumentAsync_WithLockedDocument_UnlocksDocument()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document.IsLocked = true;
        document.LockedBy = 1;
        document.LockedAt = DateTime.UtcNow;

        _context.Users.Add(user);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        await _documentService.CheckInDocumentAsync(1, 1);

        // Assert
        var updatedDocument = await _context.Documents.FindAsync(1);
        updatedDocument.Should().NotBeNull();
        updatedDocument!.IsLocked.Should().BeFalse();
        updatedDocument.LockedBy.Should().BeNull();
        updatedDocument.LockedAt.Should().BeNull();
    }

    [Fact]
    public async Task CheckInDocumentAsync_WithChangeDescription_CreatesNewVersion()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document.IsLocked = true;
        document.LockedBy = 1;
        document.CurrentVersion = 1;

        _context.Users.Add(user);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        await _documentService.CheckInDocumentAsync(1, 1, "Updated content");

        // Assert
        var updatedDocument = await _context.Documents.FindAsync(1);
        updatedDocument!.IsLocked.Should().BeFalse();
        updatedDocument.CurrentVersion.Should().Be(2);

        var newVersion = await _context.DocumentVersions
            .FirstOrDefaultAsync(v => v.DocumentId == 1 && v.VersionNumber == 2);
        newVersion.Should().NotBeNull();
        newVersion!.ChangeDescription.Should().Be("Updated content");
    }

    [Fact]
    public async Task CheckInDocumentAsync_WithUnlockedDocument_ThrowsException()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document.IsLocked = false;

        _context.Users.Add(user);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act & Assert
        var act = async () => await _documentService.CheckInDocumentAsync(1, 1);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not locked*");
    }

    [Fact]
    public async Task CheckInDocumentAsync_WithDifferentUser_ThrowsException()
    {
        // Arrange
        var user1 = TestDataBuilder.CreateUser(userId: 1);
        var user2 = TestDataBuilder.CreateUser(userId: 2);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        document.IsLocked = true;
        document.LockedBy = 1;

        _context.Users.AddRange(user1, user2);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act & Assert
        var act = async () => await _documentService.CheckInDocumentAsync(1, 2);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*cannot check in*");
    }

    [Fact]
    public async Task CheckInDocumentAsync_WithNonExistentDocument_ThrowsException()
    {
        // Arrange: لا توجد مستندات في قاعدة البيانات

        // Act & Assert
        var act = async () => await _documentService.CheckInDocumentAsync(999, 1);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region UploadDocumentAsync Tests

    [Fact]
    public async Task UploadDocumentAsync_WithValidFile_CreatesDocument()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var folder = TestDataBuilder.CreateFolder(folderId: 1);
        var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test file content"));
        var fileName = "test.pdf";

        _context.Users.Add(user);
        _context.Folders.Add(folder);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.UploadDocumentAsync(fileStream, fileName, 1, 1);

        // Assert
        result.Should().NotBeNull();
        result.DocumentId.Should().BeGreaterThan(0);
        result.DocumentName.Should().Be("test");
        // Note: FileName property doesn't exist in Document model, but DocumentService sets it internally
        // We'll verify the document was created correctly instead
        result.DocumentType.Should().Be("pdf");
        result.FolderId.Should().Be(1);
        result.UploadedBy.Should().Be(1);
        result.FilePath.Should().NotBeNullOrEmpty();
        result.FileSize.Should().Be(1024);

        // Verify file was saved
        _mockFileStorageService.Verify(
            x => x.SaveFileAsync(It.IsAny<Stream>(), fileName),
            Times.Once);
    }

    [Fact]
    public async Task UploadDocumentAsync_WithInvalidExtension_ThrowsException()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test content"));
        var fileName = "test.exe"; // Invalid extension

        // Note: DocumentService calls _fileStorageService.IsExtensionAllowed internally
        // Since IsExtensionAllowed is not in IFileStorageService interface, we need to handle this differently
        // The actual FileStorageService implementation checks against allowed extensions
        // For testing, we'll verify the exception is thrown when an invalid extension is used
        // by checking the actual behavior in DocumentService

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act & Assert
        // Since IsExtensionAllowed is called internally, we need to ensure the mock returns false
        // But since it's not in the interface, we'll test the actual behavior
        // The test will fail if IsExtensionAllowed is not properly implemented
        var act = async () => await _documentService.UploadDocumentAsync(fileStream, fileName, null, 1);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not allowed*");
    }

    [Fact]
    public async Task UploadDocumentAsync_WithPdfFile_QueuesForOcr()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("PDF content"));
        var fileName = "document.pdf";

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.UploadDocumentAsync(fileStream, fileName, null, 1);

        // Assert
        result.Should().NotBeNull();

        // Verify OCR queue was checked/created
        var ocrQueueItem = await _context.OcrQueue
            .FirstOrDefaultAsync(q => q.DocumentId == result.DocumentId);
        ocrQueueItem.Should().NotBeNull();
        ocrQueueItem!.Status.Should().Be("pending");
    }

    [Fact]
    public async Task UploadDocumentAsync_WithNonOcrFile_DoesNotQueueForOcr()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("Content"));
        var fileName = "document.docx"; // Not OCR-supported

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.UploadDocumentAsync(fileStream, fileName, null, 1);

        // Assert
        result.Should().NotBeNull();

        // Verify OCR queue is empty
        var ocrQueueItem = await _context.OcrQueue
            .FirstOrDefaultAsync(q => q.DocumentId == result.DocumentId);
        ocrQueueItem.Should().BeNull();
    }

    #endregion

    #region QueueForOcrAsync Tests

    [Fact]
    public async Task QueueForOcrAsync_WithValidDocument_CreatesOcrQueueItem()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        // Note: FileName doesn't exist in Document model, but DocumentService uses it in QueueForOcrAsync
        // We'll set FilePath to a PDF file path instead
        document.FilePath = "/test/document.pdf";

        _context.Users.Add(user);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        await _documentService.QueueForOcrAsync(1);

        // Assert
        var queueItem = await _context.OcrQueue
            .FirstOrDefaultAsync(q => q.DocumentId == 1);
        queueItem.Should().NotBeNull();
        queueItem!.Status.Should().Be("pending");
        queueItem.DocumentId.Should().Be(1);
    }

    [Fact]
    public async Task QueueForOcrAsync_WithNonExistentDocument_ThrowsException()
    {
        // Arrange: لا توجد مستندات في قاعدة البيانات

        // Act & Assert
        var act = async () => await _documentService.QueueForOcrAsync(999);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task QueueForOcrAsync_WithUnsupportedFileType_ThrowsException()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        // Note: FileName doesn't exist in Document model, but DocumentService uses it in QueueForOcrAsync
        // We'll set FilePath to a DOCX file path instead
        document.FilePath = "/test/document.docx"; // Not OCR-supported

        _context.Users.Add(user);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act & Assert
        var act = async () => await _documentService.QueueForOcrAsync(1);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not supported for OCR*");
    }

    [Fact]
    public async Task QueueForOcrAsync_WithAlreadyQueuedDocument_ThrowsException()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        // Note: FileName doesn't exist in Document model, but DocumentService uses it in QueueForOcrAsync
        // We'll set FilePath to a PDF file path instead
        document.FilePath = "/test/document.pdf";

        var existingQueueItem = new OcrQueue
        {
            DocumentId = 1,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.Documents.Add(document);
        _context.OcrQueue.Add(existingQueueItem);
        await _context.SaveChangesAsync();

        // Act & Assert
        var act = async () => await _documentService.QueueForOcrAsync(1);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already in OCR queue*");
    }

    [Fact]
    public async Task QueueForOcrAsync_WithOcrDisabled_ThrowsException()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(userId: 1);
        var document = TestDataBuilder.CreateDocument(documentId: 1, uploadedBy: 1);
        // Note: FileName doesn't exist in Document model, but DocumentService uses it in QueueForOcrAsync
        // We'll set FilePath to a PDF file path instead
        document.FilePath = "/test/document.pdf";

        _mockOcrService
            .Setup(x => x.IsOcrEnabled())
            .Returns(false);

        _context.Users.Add(user);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act & Assert
        var act = async () => await _documentService.QueueForOcrAsync(1);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not enabled*");
    }

    #endregion
}

