using FluentAssertions;
using LegalDocSystem.Middleware;
using LegalDocSystem.Models;
using LegalDocSystem.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace LegalDocSystem.Tests.Middleware;

/// <summary>
/// Unit tests for AuditLoggingMiddleware.
/// Tests HTTP request logging, user context extraction, error handling, and path skipping.
/// </summary>
public class AuditLoggingMiddlewareTests
{
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ILogger<AuditLoggingMiddleware>> _mockLogger;
    private readonly RequestDelegate _next;
    private readonly AuditLoggingMiddleware _middleware;

    public AuditLoggingMiddlewareTests()
    {
        // Setup Mocks
        _mockAuditService = new Mock<IAuditService>();
        _mockLogger = new Mock<ILogger<AuditLoggingMiddleware>>();

        // Setup default next delegate (can be overridden in individual tests)
        _next = async (context) =>
        {
            context.Response.StatusCode = 200;
            await Task.CompletedTask;
        };

        // Create Middleware instance
        _middleware = new AuditLoggingMiddleware(_next, _mockLogger.Object);
    }

    #region Successful Request Tests

    [Fact]
    public async Task InvokeAsync_WithSuccessfulRequest_LogsRequestAndResponseOnce()
    {
        // Arrange: إعداد البيانات
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/documents";
        context.Response.StatusCode = 200;

        var nextCalled = false;
        RequestDelegate next = async (ctx) =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = 200;
            await Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act: تنفيذ الوظيفة المطلوب اختبارها
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert: التحقق من النتائج
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);

        // Verify that LogEventAsync was called twice: once for start, once for completion
        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.Event == "HttpRequest" &&
                e.Action == "http_get_start" &&
                e.Category == "Document")),
            Times.Once);

        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.Event == "HttpRequest" &&
                e.Action == "http_get_completed" &&
                e.Category == "Document" &&
                e.Data != null &&
                e.Data.Contains("Status: 200"))),
            Times.Once);

        _mockAuditService.Verify(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()), Times.Exactly(2));
    }

    [Fact]
    public async Task InvokeAsync_WithPostRequest_LogsCorrectMethod()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/documents";

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 201;
            await Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert
        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.Action == "http_post_start")),
            Times.Once);

        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.Action == "http_post_completed" &&
                e.Data != null &&
                e.Data.Contains("Status: 201"))),
            Times.Once);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task InvokeAsync_WhenNextThrowsException_LogsErrorEventAndRethrows()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/documents";

        var exception = new Exception("Test exception");
        RequestDelegate next = async (ctx) =>
        {
            await Task.CompletedTask;
            throw exception;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        var act = async () => await middleware.InvokeAsync(context, _mockAuditService.Object);
        await act.Should().ThrowAsync<Exception>().WithMessage("Test exception");

        // Verify that LogEventAsync was called: once for start, once for error
        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.Action == "http_post_start")),
            Times.Once);

        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.Action == "http_post_error" &&
                e.Data != null &&
                e.Data.Contains("Error: Test exception"))),
            Times.Once);

        _mockAuditService.Verify(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()), Times.Exactly(2));
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrowsException_DoesNotLogCompletion()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/folders";

        var exception = new InvalidOperationException("Operation failed");
        RequestDelegate next = async (ctx) =>
        {
            await Task.CompletedTask;
            throw exception;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        var act = async () => await middleware.InvokeAsync(context, _mockAuditService.Object);
        await act.Should().ThrowAsync<InvalidOperationException>();

        // Verify that completion was NOT logged (only start and error)
        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.Action.Contains("_completed") && !e.Action.Contains("_error"))),
            Times.Never);
    }

    #endregion

    #region Authenticated User Tests

    [Fact]
    public async Task InvokeAsync_WithAuthenticatedUser_LogsUserContext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/documents";

        // Setup authenticated user with claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "123"),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, "admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            await Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert
        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.SubjectIdentifier == 123 &&
                e.SubjectName == "testuser" &&
                e.SubjectType == "admin")),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task InvokeAsync_WithAuthenticatedUser_LogsUserInBothStartAndCompletion()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "PUT";
        context.Request.Path = "/api/documents/1";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "456"),
            new Claim(ClaimTypes.Name, "john_doe"),
            new Claim(ClaimTypes.Role, "user")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 204;
            await Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert - Verify user info in both start and completion logs
        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.SubjectIdentifier == 456 &&
                e.SubjectName == "john_doe" &&
                e.SubjectType == "user" &&
                e.Action == "http_put_start")),
            Times.Once);

        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.SubjectIdentifier == 456 &&
                e.SubjectName == "john_doe" &&
                e.SubjectType == "user" &&
                e.Action == "http_put_completed")),
            Times.Once);
    }

    #endregion

    #region Anonymous User Tests

    [Fact]
    public async Task InvokeAsync_WithAnonymousUser_LogsWithoutUserId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/documents";
        context.User = new ClaimsPrincipal(); // Empty principal (anonymous)

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            await Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert
        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.SubjectIdentifier == null &&
                e.SubjectName == null &&
                e.SubjectType == null)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task InvokeAsync_WithNullUser_LogsWithoutUserId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/tasks";
        // User is null by default in DefaultHttpContext

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 201;
            await Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert
        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.SubjectIdentifier == null)),
            Times.AtLeastOnce);
    }

    #endregion

    #region Response Integrity Tests

    [Fact]
    public async Task InvokeAsync_DoesNotModifyResponseBodyOrStatusCodeUnexpectedly()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/documents";

        var responseBody = "Test Response Body";
        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            var bytes = Encoding.UTF8.GetBytes(responseBody);
            await ctx.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert
        context.Response.StatusCode.Should().Be(200);
        context.Response.ContentType.Should().Be("application/json");

        // Verify response body was not modified
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var bodyContent = await reader.ReadToEndAsync();
        bodyContent.Should().Be(responseBody);
    }

    [Fact]
    public async Task InvokeAsync_PreservesResponseStatusCode()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/documents";

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 404;
            await Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert
        context.Response.StatusCode.Should().Be(404);

        // Verify that the status code was logged correctly
        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.Data != null &&
                e.Data.Contains("Status: 404"))),
            Times.Once);
    }

    #endregion

    #region Path Skipping Tests

    [Fact]
    public async Task InvokeAsync_WithStaticFilePath_SkipsLogging()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/css/styles.css";

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            await Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert
        context.Response.StatusCode.Should().Be(200);
        _mockAuditService.Verify(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithHealthCheckPath_SkipsLogging()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/health";

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            await Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert
        context.Response.StatusCode.Should().Be(200);
        _mockAuditService.Verify(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithFaviconPath_SkipsLogging()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/favicon.ico";

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            await Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert
        context.Response.StatusCode.Should().Be(200);
        _mockAuditService.Verify(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()), Times.Never);
    }

    #endregion

    #region Category Determination Tests

    [Fact]
    public async Task InvokeAsync_WithDocumentPath_LogsWithDocumentCategory()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/documents/1";

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            await Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert
        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.Category == "Document")),
            Times.Exactly(2));
    }

    [Fact]
    public async Task InvokeAsync_WithFolderPath_LogsWithFolderCategory()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/folders";

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 201;
            await Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert
        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.Category == "Folder")),
            Times.Exactly(2));
    }

    [Fact]
    public async Task InvokeAsync_WithLoginPath_LogsWithAuthenticationCategory()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/login";

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            await Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert
        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.Category == "Authentication")),
            Times.Exactly(2));
    }

    [Fact]
    public async Task InvokeAsync_WithUnknownApiPath_LogsWithApiCategory()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/unknown";

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            await Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert
        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.Category == "API")),
            Times.Exactly(2));
    }

    [Fact]
    public async Task InvokeAsync_WithNonApiPath_LogsWithGeneralCategory()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/some/other/path";

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            await Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert
        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.Category == "General")),
            Times.Exactly(2));
    }

    #endregion

    #region IP Address and User Agent Tests

    [Fact]
    public async Task InvokeAsync_WithXForwardedForHeader_UsesForwardedIpAddress()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/documents";
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.100";

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            await Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert
        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.IpAddress == "192.168.1.100")),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task InvokeAsync_WithUserAgentHeader_LogsUserAgent()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/documents";
        context.Request.Headers["User-Agent"] = "Mozilla/5.0 (Test Browser)";

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            await Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert
        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.UserAgent == "Mozilla/5.0 (Test Browser)")),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task InvokeAsync_WithoutUserAgentHeader_LogsUnknown()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/documents";
        // No User-Agent header

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            await Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert
        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.UserAgent == "Unknown")),
            Times.AtLeastOnce);
    }

    #endregion

    #region Query String Tests

    [Fact]
    public async Task InvokeAsync_WithQueryString_IncludesQueryStringInLog()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/documents";
        context.Request.QueryString = new QueryString("?page=1&size=10");

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            await Task.CompletedTask;
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert
        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.Data != null &&
                e.Data.Contains("/api/documents?page=1&size=10"))),
            Times.AtLeastOnce);
    }

    #endregion

    #region Duration Tests

    [Fact]
    public async Task InvokeAsync_LogsRequestDuration()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/documents";

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            await Task.Delay(100); // Simulate some processing time
        };

        var middleware = new AuditLoggingMiddleware(next, _mockLogger.Object);

        _mockAuditService
            .Setup(x => x.LogEventAsync(It.IsAny<AuditLogEntry>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, _mockAuditService.Object);

        // Assert
        _mockAuditService.Verify(
            x => x.LogEventAsync(It.Is<AuditLogEntry>(e =>
                e.Data != null &&
                e.Data.Contains("Duration:") &&
                e.Action == "http_get_completed")),
            Times.Once);
    }

    #endregion
}


