using FluentAssertions;
using LegalDocSystem.Controllers;
using LegalDocSystem.Models;
using LegalDocSystem.Services;
using LegalDocSystem.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace LegalDocSystem.Tests.Controllers;

/// <summary>
/// Unit tests for FoldersController.
/// Tests API endpoints for folder management operations including CRUD, subfolders, and authorization.
/// </summary>
public class FoldersControllerTests
{
    private readonly Mock<IFolderService> _mockFolderService;
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly FoldersController _controller;

    public FoldersControllerTests()
    {
        // Setup Mocks
        _mockFolderService = new Mock<IFolderService>();
        _mockAuthService = new Mock<IAuthService>();

        // Create Controller instance
        _controller = new FoldersController(
            _mockFolderService.Object,
            _mockAuthService.Object);

        // Setup default ControllerContext for testing
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithExistingFolders_ReturnsOkWithFolders()
    {
        // Arrange: إعداد البيانات
        var folders = new List<Folder>
        {
            TestDataBuilder.CreateFolder(folderId: 1),
            TestDataBuilder.CreateFolder(folderId: 2),
            TestDataBuilder.CreateFolder(folderId: 3)
        };

        _mockFolderService
            .Setup(x => x.GetAllFoldersAsync())
            .ReturnsAsync(folders);

        // Act: تنفيذ الوظيفة المطلوب اختبارها
        var result = await _controller.GetAll();

        // Assert: التحقق من النتائج
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedFolders = okResult.Value.Should().BeAssignableTo<List<Folder>>().Subject;
        returnedFolders.Should().HaveCount(3);
        
        _mockFolderService.Verify(x => x.GetAllFoldersAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithNoFolders_ReturnsOkWithEmptyList()
    {
        // Arrange
        _mockFolderService
            .Setup(x => x.GetAllFoldersAsync())
            .ReturnsAsync(new List<Folder>());

        // Act
        var result = await _controller.GetAll();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedFolders = okResult.Value.Should().BeAssignableTo<List<Folder>>().Subject;
        returnedFolders.Should().BeEmpty();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithExistingId_ReturnsOkWithFolder()
    {
        // Arrange
        var folder = TestDataBuilder.CreateFolder(folderId: 1);
        folder.FolderName = "Test Folder";

        _mockFolderService
            .Setup(x => x.GetFolderByIdAsync(1))
            .ReturnsAsync(folder);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedFolder = okResult.Value.Should().BeAssignableTo<Folder>().Subject;
        returnedFolder.FolderId.Should().Be(1);
        returnedFolder.FolderName.Should().Be("Test Folder");
        
        _mockFolderService.Verify(x => x.GetFolderByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        _mockFolderService
            .Setup(x => x.GetFolderByIdAsync(999))
            .ReturnsAsync((Folder?)null);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<NotFoundResult>();
        
        _mockFolderService.Verify(x => x.GetFolderByIdAsync(999), Times.Once);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidFolderAndAuthenticatedUser_ReturnsCreated()
    {
        // Arrange
        var folder = TestDataBuilder.CreateFolder(folderId: 0);
        folder.FolderName = "New Folder";

        var createdFolder = TestDataBuilder.CreateFolder(folderId: 1);
        createdFolder.FolderName = "New Folder";

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockFolderService
            .Setup(x => x.CreateFolderAsync(It.IsAny<Folder>()))
            .ReturnsAsync(createdFolder);

        // Act
        var result = await _controller.Create(folder);

        // Assert
        result.Should().NotBeNull();
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(FoldersController.GetById));
        createdResult.RouteValues!["id"].Should().Be(1);
        createdResult.Value.Should().BeAssignableTo<Folder>();
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
        _mockFolderService.Verify(x => x.CreateFolderAsync(It.IsAny<Folder>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var folder = TestDataBuilder.CreateFolder(folderId: 0);

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Create(folder);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<UnauthorizedResult>();
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
        _mockFolderService.Verify(x => x.CreateFolderAsync(It.IsAny<Folder>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var folder = TestDataBuilder.CreateFolder(folderId: 0);

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockFolderService
            .Setup(x => x.CreateFolderAsync(It.IsAny<Folder>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Create(folder);

        // Assert
        result.Should().NotBeNull();
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An error occurred while creating the folder. Please try again later.");
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidFolderAndAuthenticatedUser_ReturnsNoContent()
    {
        // Arrange
        var folder = TestDataBuilder.CreateFolder(folderId: 1);
        folder.FolderName = "Updated Folder";

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockFolderService
            .Setup(x => x.UpdateFolderAsync(It.IsAny<Folder>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Update(1, folder);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<NoContentResult>();
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
        _mockFolderService.Verify(x => x.UpdateFolderAsync(It.Is<Folder>(f => f.FolderId == 1)), Times.Once);
    }

    [Fact]
    public async Task Update_WithMismatchedId_ReturnsBadRequest()
    {
        // Arrange
        var folder = TestDataBuilder.CreateFolder(folderId: 1);

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Update(2, folder);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<BadRequestResult>();
        
        _mockFolderService.Verify(x => x.UpdateFolderAsync(It.IsAny<Folder>()), Times.Never);
    }

    [Fact]
    public async Task Update_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var folder = TestDataBuilder.CreateFolder(folderId: 1);

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Update(1, folder);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<UnauthorizedResult>();
        
        _mockFolderService.Verify(x => x.UpdateFolderAsync(It.IsAny<Folder>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var folder = TestDataBuilder.CreateFolder(folderId: 1);

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockFolderService
            .Setup(x => x.UpdateFolderAsync(It.IsAny<Folder>()))
            .ThrowsAsync(new Exception("Update error"));

        // Act
        var result = await _controller.Update(1, folder);

        // Assert
        result.Should().NotBeNull();
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An error occurred while updating the folder. Please try again later.");
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithExistingIdAndAdminUser_ReturnsNoContent()
    {
        // Arrange
        var adminUser = TestDataBuilder.CreateUser(userId: 1, role: "admin");

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockAuthService
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(adminUser);

        _mockFolderService
            .Setup(x => x.DeleteFolderAsync(1))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<NoContentResult>();
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
        _mockAuthService.Verify(x => x.GetCurrentUserAsync(), Times.Once);
        _mockFolderService.Verify(x => x.DeleteFolderAsync(1), Times.Once);
    }

    [Fact]
    public async Task Delete_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<UnauthorizedResult>();
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
        _mockAuthService.Verify(x => x.GetCurrentUserAsync(), Times.Never);
        _mockFolderService.Verify(x => x.DeleteFolderAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WithNonAdminUser_ReturnsUnauthorized()
    {
        // Arrange
        var regularUser = TestDataBuilder.CreateUser(userId: 1, role: "user");

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockAuthService
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(regularUser);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().NotBeNull();
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().Be("Only administrators can delete folders");
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
        _mockAuthService.Verify(x => x.GetCurrentUserAsync(), Times.Once);
        _mockFolderService.Verify(x => x.DeleteFolderAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var adminUser = TestDataBuilder.CreateUser(userId: 1, role: "admin");

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockAuthService
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(adminUser);

        _mockFolderService
            .Setup(x => x.DeleteFolderAsync(1))
            .ThrowsAsync(new Exception("Delete error"));

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().NotBeNull();
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An error occurred while deleting the folder. Please try again later.");
    }

    #endregion

    #region GetSubFolders Tests

    [Fact]
    public async Task GetSubFolders_WithExistingParent_ReturnsOkWithSubFolders()
    {
        // Arrange
        var subFolders = new List<Folder>
        {
            TestDataBuilder.CreateFolder(folderId: 2, parentId: 1),
            TestDataBuilder.CreateFolder(folderId: 3, parentId: 1)
        };

        _mockFolderService
            .Setup(x => x.GetSubFoldersAsync(1))
            .ReturnsAsync(subFolders);

        // Act
        var result = await _controller.GetSubFolders(1);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedFolders = okResult.Value.Should().BeAssignableTo<List<Folder>>().Subject;
        returnedFolders.Should().HaveCount(2);
        
        _mockFolderService.Verify(x => x.GetSubFoldersAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetSubFolders_WithNoSubFolders_ReturnsOkWithEmptyList()
    {
        // Arrange
        _mockFolderService
            .Setup(x => x.GetSubFoldersAsync(1))
            .ReturnsAsync(new List<Folder>());

        // Act
        var result = await _controller.GetSubFolders(1);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedFolders = okResult.Value.Should().BeAssignableTo<List<Folder>>().Subject;
        returnedFolders.Should().BeEmpty();
    }

    #endregion

    #region GetRootFolders Tests

    [Fact]
    public async Task GetRootFolders_WithRootFolders_ReturnsOkWithRootFolders()
    {
        // Arrange
        var allFolders = new List<Folder>
        {
            TestDataBuilder.CreateFolder(folderId: 1, parentId: null),
            TestDataBuilder.CreateFolder(folderId: 2, parentId: null),
            TestDataBuilder.CreateFolder(folderId: 3, parentId: 1) // Child folder
        };

        _mockFolderService
            .Setup(x => x.GetAllFoldersAsync())
            .ReturnsAsync(allFolders);

        // Act
        var result = await _controller.GetRootFolders();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedFolders = okResult.Value.Should().BeAssignableTo<List<Folder>>().Subject;
        returnedFolders.Should().HaveCount(2);
        returnedFolders.Should().OnlyContain(f => f.ParentId == null);
        
        _mockFolderService.Verify(x => x.GetAllFoldersAsync(), Times.Once);
    }

    [Fact]
    public async Task GetRootFolders_WithNoRootFolders_ReturnsOkWithEmptyList()
    {
        // Arrange
        var allFolders = new List<Folder>
        {
            TestDataBuilder.CreateFolder(folderId: 1, parentId: 1) // All have parents
        };

        _mockFolderService
            .Setup(x => x.GetAllFoldersAsync())
            .ReturnsAsync(allFolders);

        // Act
        var result = await _controller.GetRootFolders();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedFolders = okResult.Value.Should().BeAssignableTo<List<Folder>>().Subject;
        returnedFolders.Should().BeEmpty();
    }

    #endregion
}


