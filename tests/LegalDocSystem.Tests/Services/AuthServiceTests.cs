using FluentAssertions;
using LegalDocSystem.Data;
using LegalDocSystem.Models;
using LegalDocSystem.Services;
using LegalDocSystem.Tests.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Xunit;

namespace LegalDocSystem.Tests.Services;

/// <summary>
/// Unit tests for AuthService.
/// Tests authentication, authorization, and account lockout functionality.
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IAuthenticationService> _mockAuthenticationService;
    private readonly AuthService _authService;
    private readonly HttpContext _httpContext;
    private readonly Mock<ISession> _mockSession;

    public AuthServiceTests()
    {
        // Setup In-Memory Database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Setup Mocks
        _mockUserService = new Mock<IUserService>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockAuditService = new Mock<IAuditService>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockAuthenticationService = new Mock<IAuthenticationService>();
        _mockSession = new Mock<ISession>();

        // Setup HttpContext and Session
        _httpContext = new DefaultHttpContext();
        _httpContext.Session = _mockSession.Object;
        _httpContext.RequestServices = _mockServiceProvider.Object;

        // Setup Session methods
        var sessionData = new Dictionary<string, byte[]>();
        _mockSession.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Callback<string, byte[]>((key, value) => sessionData[key] = value);
        _mockSession.Setup(s => s.GetInt32(It.IsAny<string>()))
            .Returns<string>(key =>
            {
                if (sessionData.TryGetValue(key, out var bytes) && bytes.Length == 4)
                    return BitConverter.ToInt32(bytes, 0);
                return null;
            });
        _mockSession.Setup(s => s.GetString(It.IsAny<string>()))
            .Returns<string>(key =>
            {
                if (sessionData.TryGetValue(key, out var bytes))
                    return System.Text.Encoding.UTF8.GetString(bytes);
                return null;
            });
        _mockSession.Setup(s => s.Clear()).Callback(() => sessionData.Clear());

        // Setup Authentication Service
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
            .Returns(_mockAuthenticationService.Object);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContext);

        // Setup Authentication Service methods
        _mockAuthenticationService
            .Setup(x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        _mockAuthenticationService
            .Setup(x => x.SignOutAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        // Create AuthService instance
        _authService = new AuthService(
            _mockUserService.Object,
            _mockHttpContextAccessor.Object,
            _mockAuditService.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsUser()
    {
        // Arrange: إعداد البيانات والـ Mocks
        var username = "testuser";
        var password = "TestPassword123";
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: username,
            password: password);

        _mockUserService
            .Setup(x => x.GetUserByUsernameAsync(username))
            .ReturnsAsync(user);

        _mockUserService
            .Setup(x => x.IsAccountLockedAsync(username))
            .ReturnsAsync(false);

        _mockUserService
            .Setup(x => x.ValidatePasswordAsync(username, password))
            .ReturnsAsync(true);

        _mockUserService
            .Setup(x => x.ResetFailedLoginAttemptsAsync(user.UserId))
            .Returns(Task.CompletedTask);

        // Act: تنفيذ الوظيفة المطلوب اختبارها
        var result = await _authService.LoginAsync(username, password);

        // Assert: التحقق من النتائج
        result.Should().NotBeNull();
        result!.Username.Should().Be(username);
        result.UserId.Should().Be(user.UserId);

        // Verify: التحقق من استدعاء الـ Dependencies
        _mockAuthenticationService.Verify(
            x => x.SignInAsync(
                _httpContext,
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Once);

        _mockAuditService.Verify(
            x => x.LogLoginAsync(
                user.UserId,
                username,
                true,
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsNull()
    {
        // Arrange
        var username = "testuser";
        var password = "WrongPassword";
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: username,
            password: "CorrectPassword");

        _mockUserService
            .Setup(x => x.GetUserByUsernameAsync(username))
            .ReturnsAsync(user);

        _mockUserService
            .Setup(x => x.IsAccountLockedAsync(username))
            .ReturnsAsync(false);

        _mockUserService
            .Setup(x => x.ValidatePasswordAsync(username, password))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.LoginAsync(username, password);

        // Assert
        result.Should().BeNull();

        // Verify SignInAsync was NOT called
        _mockAuthenticationService.Verify(
            x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Never);

        // Verify Audit Log was called for failed login
        _mockAuditService.Verify(
            x => x.LogLoginAsync(
                user.UserId,
                username,
                false,
                It.Is<string>(s => s.Contains("Invalid password") || s.Contains("فاشل"))),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithLockedAccount_ReturnsNull()
    {
        // Arrange
        var username = "testuser";
        var password = "TestPassword123";
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: username,
            password: password);
        user.LockedUntil = DateTime.UtcNow.AddMinutes(15);

        _mockUserService
            .Setup(x => x.GetUserByUsernameAsync(username))
            .ReturnsAsync(user);

        _mockUserService
            .Setup(x => x.IsAccountLockedAsync(username))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.LoginAsync(username, password);

        // Assert
        result.Should().BeNull();

        // Verify SignInAsync was NOT called
        _mockAuthenticationService.Verify(
            x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Never);

        // Verify Audit Log was called for account lockout
        _mockAuditService.Verify(
            x => x.LogLoginAsync(
                user.UserId,
                username,
                false,
                It.Is<string>(s => s.Contains("Account locked") || s.Contains("قفل"))),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ReturnsNull()
    {
        // Arrange
        var username = "testuser";
        var password = "TestPassword123";
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: username,
            password: password,
            isActive: false);

        _mockUserService
            .Setup(x => x.GetUserByUsernameAsync(username))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(username, password);

        // Assert
        result.Should().BeNull();

        // Verify Audit Log was called
        _mockAuditService.Verify(
            x => x.LogLoginAsync(
                It.IsAny<int?>(),
                username,
                false,
                It.Is<string>(s => s.Contains("inactive") || s.Contains("غير نشط"))),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ReturnsNull()
    {
        // Arrange
        var username = "nonexistent";
        var password = "TestPassword123";

        _mockUserService
            .Setup(x => x.GetUserByUsernameAsync(username))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(username, password);

        // Assert
        result.Should().BeNull();

        // Verify Audit Log was called
        _mockAuditService.Verify(
            x => x.LogLoginAsync(
                null,
                username,
                false,
                It.Is<string>(s => s.Contains("not found") || s.Contains("غير موجود"))),
            Times.Once);
    }

    #endregion

    #region LoginWithResultAsync Tests

    [Fact]
    public async Task LoginWithResultAsync_WithValidCredentials_ReturnsSuccessResult()
    {
        // Arrange
        var username = "testuser";
        var password = "TestPassword123";
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: username,
            password: password);

        _mockUserService
            .Setup(x => x.GetUserByUsernameAsync(username))
            .ReturnsAsync(user);

        _mockUserService
            .Setup(x => x.IsAccountLockedAsync(username))
            .ReturnsAsync(false);

        _mockUserService
            .Setup(x => x.ValidatePasswordAsync(username, password))
            .ReturnsAsync(true);

        _mockUserService
            .Setup(x => x.ResetFailedLoginAttemptsAsync(user.UserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.LoginWithResultAsync(username, password);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be(username);
        result.IsAccountLocked.Should().BeFalse();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task LoginWithResultAsync_WithLockedAccount_ReturnsLockoutInfo()
    {
        // Arrange
        var username = "testuser";
        var password = "TestPassword123";
        var lockoutExpiration = DateTime.UtcNow.AddMinutes(15);
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: username,
            password: password);
        user.LockedUntil = lockoutExpiration;

        _mockUserService
            .Setup(x => x.GetUserByUsernameAsync(username))
            .ReturnsAsync(user);

        _mockUserService
            .Setup(x => x.IsAccountLockedAsync(username))
            .ReturnsAsync(true);

        _mockUserService
            .Setup(x => x.GetLockoutExpirationAsync(username))
            .ReturnsAsync(lockoutExpiration);

        // Act
        var result = await _authService.LoginWithResultAsync(username, password);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.IsAccountLocked.Should().BeTrue();
        result.LockoutExpiration.Should().Be(lockoutExpiration);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().Contain("قفل");
    }

    [Fact]
    public async Task LoginWithResultAsync_WithInvalidPassword_ReturnsFailureResult()
    {
        // Arrange
        var username = "testuser";
        var password = "WrongPassword";
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: username,
            password: "CorrectPassword");

        _mockUserService
            .Setup(x => x.GetUserByUsernameAsync(username))
            .ReturnsAsync(user);

        _mockUserService
            .Setup(x => x.IsAccountLockedAsync(username))
            .ReturnsAsync(false);

        _mockUserService
            .Setup(x => x.ValidatePasswordAsync(username, password))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.LoginWithResultAsync(username, password);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.IsAccountLocked.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.User.Should().BeNull();
    }

    #endregion

    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_ClearsSessionAndSignsOut()
    {
        // Arrange
        var userId = 1;
        var username = "testuser";

        // Setup authenticated user in HttpContext
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        _httpContext.User = new ClaimsPrincipal(identity);

        // Setup Session to return user data
        _mockSession.Setup(s => s.GetInt32("UserId")).Returns(userId);
        _mockSession.Setup(s => s.GetString("Username")).Returns(username);

        // Act
        await _authService.LogoutAsync();

        // Assert
        // Verify SignOutAsync was called
        _mockAuthenticationService.Verify(
            x => x.SignOutAsync(
                _httpContext,
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<AuthenticationProperties>()),
            Times.Once);

        // Verify Session.Clear was called
        _mockSession.Verify(s => s.Clear(), Times.Once);
    }

    #endregion

    #region IsAuthenticatedAsync Tests

    [Fact]
    public async Task IsAuthenticatedAsync_WithAuthenticatedUser_ReturnsTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "testuser")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        _httpContext.User = new ClaimsPrincipal(identity);

        // Setup Session to return userId
        _mockSession.Setup(s => s.GetInt32("UserId")).Returns(1);

        // Act
        var result = await _authService.IsAuthenticatedAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAuthenticatedAsync_WithUnauthenticatedUser_ReturnsFalse()
    {
        // Arrange
        _httpContext.User = new ClaimsPrincipal();
        _mockSession.Setup(s => s.GetInt32("UserId")).Returns((int?)null);

        // Act
        var result = await _authService.IsAuthenticatedAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetCurrentUserAsync Tests

    [Fact]
    public async Task GetCurrentUserAsync_WithValidUserId_ReturnsUser()
    {
        // Arrange
        var userId = 1;
        var username = "testuser";
        var user = TestDataBuilder.CreateUser(
            userId: userId,
            username: username);

        // Setup Session to return userId
        _mockSession.Setup(s => s.GetInt32("UserId")).Returns(userId);

        _mockUserService
            .Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.GetCurrentUserAsync();

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.Username.Should().Be(username);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithInvalidUserId_ReturnsNull()
    {
        // Arrange
        _mockSession.Setup(s => s.GetInt32("UserId")).Returns((int?)null);

        // Act
        var result = await _authService.GetCurrentUserAsync();

        // Assert
        result.Should().BeNull();
    }

    #endregion
}

