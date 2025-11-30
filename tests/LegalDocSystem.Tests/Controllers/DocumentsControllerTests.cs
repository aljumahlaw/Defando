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
/// Unit tests for DocumentsController.
/// Tests API endpoints for document management operations including CRUD, search, and authorization.
/// </summary>
public class DocumentsControllerTests
{
    private readonly Mock<IDocumentService> _mockDocumentService;
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly DocumentsController _controller;

    public DocumentsControllerTests()
    {
        // Setup Mocks
        _mockDocumentService = new Mock<IDocumentService>();
        _mockAuthService = new Mock<IAuthService>();

        // Create Controller instance
        _controller = new DocumentsController(
            _mockDocumentService.Object,
            _mockAuthService.Object);

        // Setup default ControllerContext for testing
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithExistingDocuments_ReturnsOkWithDocuments()
    {
        // Arrange: إعداد البيانات
        var documents = new List<Document>
        {
            TestDataBuilder.CreateDocument(documentId: 1),
            TestDataBuilder.CreateDocument(documentId: 2),
            TestDataBuilder.CreateDocument(documentId: 3)
        };

        _mockDocumentService
            .Setup(x => x.GetAllDocumentsAsync())
            .ReturnsAsync(documents);

        // Act: تنفيذ الوظيفة المطلوب اختبارها
        var result = await _controller.GetAll();

        // Assert: التحقق من النتائج
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDocuments = okResult.Value.Should().BeAssignableTo<List<Document>>().Subject;
        returnedDocuments.Should().HaveCount(3);
        
        _mockDocumentService.Verify(x => x.GetAllDocumentsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithNoDocuments_ReturnsOkWithEmptyList()
    {
        // Arrange
        _mockDocumentService
            .Setup(x => x.GetAllDocumentsAsync())
            .ReturnsAsync(new List<Document>());

        // Act
        var result = await _controller.GetAll();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDocuments = okResult.Value.Should().BeAssignableTo<List<Document>>().Subject;
        returnedDocuments.Should().BeEmpty();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithExistingId_ReturnsOkWithDocument()
    {
        // Arrange
        var document = TestDataBuilder.CreateDocument(documentId: 1);
        document.DocumentName = "Test Document";

        _mockDocumentService
            .Setup(x => x.GetDocumentByIdAsync(1))
            .ReturnsAsync(document);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDocument = okResult.Value.Should().BeAssignableTo<Document>().Subject;
        returnedDocument.DocumentId.Should().Be(1);
        returnedDocument.DocumentName.Should().Be("Test Document");
        
        _mockDocumentService.Verify(x => x.GetDocumentByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        _mockDocumentService
            .Setup(x => x.GetDocumentByIdAsync(999))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<NotFoundResult>();
        
        _mockDocumentService.Verify(x => x.GetDocumentByIdAsync(999), Times.Once);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidDocumentAndAuthenticatedUser_ReturnsCreated()
    {
        // Arrange
        var document = TestDataBuilder.CreateDocument(documentId: 0);
        document.DocumentName = "New Document";

        var createdDocument = TestDataBuilder.CreateDocument(documentId: 1);
        createdDocument.DocumentName = "New Document";

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockDocumentService
            .Setup(x => x.CreateDocumentAsync(It.IsAny<Document>()))
            .ReturnsAsync(createdDocument);

        // Act
        var result = await _controller.Create(document);

        // Assert
        result.Should().NotBeNull();
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(DocumentsController.GetById));
        createdResult.RouteValues!["id"].Should().Be(1);
        createdResult.Value.Should().BeAssignableTo<Document>();
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
        _mockDocumentService.Verify(x => x.CreateDocumentAsync(It.IsAny<Document>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var document = TestDataBuilder.CreateDocument(documentId: 0);

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Create(document);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<UnauthorizedResult>();
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
        _mockDocumentService.Verify(x => x.CreateDocumentAsync(It.IsAny<Document>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var document = TestDataBuilder.CreateDocument(documentId: 0);

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockDocumentService
            .Setup(x => x.CreateDocumentAsync(It.IsAny<Document>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Create(document);

        // Assert
        result.Should().NotBeNull();
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An error occurred while creating the document. Please try again later.");
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidDocumentAndAuthenticatedUser_ReturnsNoContent()
    {
        // Arrange
        var document = TestDataBuilder.CreateDocument(documentId: 1);
        document.DocumentName = "Updated Document";

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockDocumentService
            .Setup(x => x.UpdateDocumentAsync(It.IsAny<Document>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Update(1, document);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<NoContentResult>();
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
        _mockDocumentService.Verify(x => x.UpdateDocumentAsync(It.Is<Document>(d => d.DocumentId == 1)), Times.Once);
    }

    [Fact]
    public async Task Update_WithMismatchedId_ReturnsBadRequest()
    {
        // Arrange
        var document = TestDataBuilder.CreateDocument(documentId: 1);

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Update(2, document);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<BadRequestResult>();
        
        _mockDocumentService.Verify(x => x.UpdateDocumentAsync(It.IsAny<Document>()), Times.Never);
    }

    [Fact]
    public async Task Update_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var document = TestDataBuilder.CreateDocument(documentId: 1);

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Update(1, document);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<UnauthorizedResult>();
        
        _mockDocumentService.Verify(x => x.UpdateDocumentAsync(It.IsAny<Document>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var document = TestDataBuilder.CreateDocument(documentId: 1);

        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockDocumentService
            .Setup(x => x.UpdateDocumentAsync(It.IsAny<Document>()))
            .ThrowsAsync(new Exception("Update error"));

        // Act
        var result = await _controller.Update(1, document);

        // Assert
        result.Should().NotBeNull();
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An error occurred while updating the document. Please try again later.");
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

        _mockDocumentService
            .Setup(x => x.DeleteDocumentAsync(1))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<NoContentResult>();
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
        _mockAuthService.Verify(x => x.GetCurrentUserAsync(), Times.Once);
        _mockDocumentService.Verify(x => x.DeleteDocumentAsync(1), Times.Once);
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
        _mockDocumentService.Verify(x => x.DeleteDocumentAsync(It.IsAny<int>()), Times.Never);
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
        unauthorizedResult.Value.Should().Be("Only administrators can delete documents");
        
        _mockAuthService.Verify(x => x.IsAuthenticatedAsync(), Times.Once);
        _mockAuthService.Verify(x => x.GetCurrentUserAsync(), Times.Once);
        _mockDocumentService.Verify(x => x.DeleteDocumentAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WithNullCurrentUser_ReturnsUnauthorized()
    {
        // Arrange
        _mockAuthService
            .Setup(x => x.IsAuthenticatedAsync())
            .ReturnsAsync(true);

        _mockAuthService
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().NotBeNull();
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().Be("Only administrators can delete documents");
        
        _mockDocumentService.Verify(x => x.DeleteDocumentAsync(It.IsAny<int>()), Times.Never);
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

        _mockDocumentService
            .Setup(x => x.DeleteDocumentAsync(1))
            .ThrowsAsync(new Exception("Delete error"));

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().NotBeNull();
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An error occurred while deleting the document. Please try again later.");
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task Search_WithValidQuery_ReturnsOkWithResults()
    {
        // Arrange
        var query = "test";
        var searchResults = new List<Document>
        {
            TestDataBuilder.CreateDocument(documentId: 1),
            TestDataBuilder.CreateDocument(documentId: 2)
        };

        _mockDocumentService
            .Setup(x => x.SearchDocumentsAsync(query))
            .ReturnsAsync(searchResults);

        // Act
        var result = await _controller.Search(query);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDocuments = okResult.Value.Should().BeAssignableTo<List<Document>>().Subject;
        returnedDocuments.Should().HaveCount(2);
        
        _mockDocumentService.Verify(x => x.SearchDocumentsAsync(query), Times.Once);
    }

    [Fact]
    public async Task Search_WithEmptyQuery_ReturnsBadRequest()
    {
        // Arrange
        var query = "";

        // Act
        var result = await _controller.Search(query);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Query parameter is required");
        
        _mockDocumentService.Verify(x => x.SearchDocumentsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Search_WithWhitespaceQuery_ReturnsBadRequest()
    {
        // Arrange
        var query = "   ";

        // Act
        var result = await _controller.Search(query);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Query parameter is required");
        
        _mockDocumentService.Verify(x => x.SearchDocumentsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Search_WithNullQuery_ReturnsBadRequest()
    {
        // Arrange
        string? query = null;

        // Act
        var result = await _controller.Search(query!);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Query parameter is required");
        
        _mockDocumentService.Verify(x => x.SearchDocumentsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Search_WithNoResults_ReturnsOkWithEmptyList()
    {
        // Arrange
        var query = "nonexistent";

        _mockDocumentService
            .Setup(x => x.SearchDocumentsAsync(query))
            .ReturnsAsync(new List<Document>());

        // Act
        var result = await _controller.Search(query);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDocuments = okResult.Value.Should().BeAssignableTo<List<Document>>().Subject;
        returnedDocuments.Should().BeEmpty();
    }

    #endregion
}

