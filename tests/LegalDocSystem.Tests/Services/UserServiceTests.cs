using FluentAssertions;
using LegalDocSystem.Data;
using LegalDocSystem.Models;
using LegalDocSystem.Services;
using LegalDocSystem.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace LegalDocSystem.Tests.Services;

/// <summary>
/// Unit tests for UserService.
/// Tests user management, password validation, and account lockout functionality.
/// </summary>
public class UserServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        // Setup In-Memory Database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Setup Mocks
        _mockAuditService = new Mock<IAuditService>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Setup Configuration Mock for Account Lockout settings
        var configurationSection = new Mock<IConfigurationSection>();
        configurationSection.Setup(x => x["MaxFailedAttempts"]).Returns("5");
        configurationSection.Setup(x => x["LockoutDurationMinutes"]).Returns("15");
        configurationSection.Setup(x => x["EnableAutoUnlock"]).Returns("true");

        _mockConfiguration.Setup(x => x.GetSection("AccountLockout")).Returns(configurationSection.Object);
        _mockConfiguration.Setup(x => x.GetValue<int>("AccountLockout:MaxFailedAttempts", It.IsAny<int>())).Returns(5);
        _mockConfiguration.Setup(x => x.GetValue<int>("AccountLockout:LockoutDurationMinutes", It.IsAny<int>())).Returns(15);
        _mockConfiguration.Setup(x => x.GetValue<bool>("AccountLockout:EnableAutoUnlock", It.IsAny<bool>())).Returns(true);

        // Setup Audit Service Mock
        _mockAuditService
            .Setup(x => x.LogCreateAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Create UserService instance
        _userService = new UserService(
            _context,
            _mockAuditService.Object,
            _mockConfiguration.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetAllUsersAsync Tests

    [Fact]
    public async Task GetAllUsersAsync_WithActiveUsers_ReturnsOnlyActiveUsers()
    {
        // Arrange: إعداد البيانات
        var activeUser1 = TestDataBuilder.CreateUser(
            userId: 1,
            username: "user1",
            isActive: true);
        var activeUser2 = TestDataBuilder.CreateUser(
            userId: 2,
            username: "user2",
            isActive: true);
        var inactiveUser = TestDataBuilder.CreateUser(
            userId: 3,
            username: "user3",
            isActive: false);

        _context.Users.AddRange(activeUser1, activeUser2, inactiveUser);
        await _context.SaveChangesAsync();

        // Act: تنفيذ الوظيفة المطلوب اختبارها
        var result = await _userService.GetAllUsersAsync();

        // Assert: التحقق من النتائج
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(u => u.IsActive == true);
        result.Should().Contain(u => u.Username == "user1");
        result.Should().Contain(u => u.Username == "user2");
        result.Should().NotContain(u => u.Username == "user3");
    }

    [Fact]
    public async Task GetAllUsersAsync_WithNoUsers_ReturnsEmptyList()
    {
        // Arrange: لا توجد مستخدمين في قاعدة البيانات

        // Act
        var result = await _userService.GetAllUsersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllUsersAsync_OrdersByFullName()
    {
        // Arrange
        var user1 = TestDataBuilder.CreateUser(
            userId: 1,
            username: "user1");
        user1.FullName = "Zebra User";

        var user2 = TestDataBuilder.CreateUser(
            userId: 2,
            username: "user2");
        user2.FullName = "Alpha User";

        var user3 = TestDataBuilder.CreateUser(
            userId: 3,
            username: "user3");
        user3.FullName = "Beta User";

        _context.Users.AddRange(user1, user2, user3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetAllUsersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result[0].FullName.Should().Be("Alpha User");
        result[1].FullName.Should().Be("Beta User");
        result[2].FullName.Should().Be("Zebra User");
    }

    #endregion

    #region GetUserByIdAsync Tests

    [Fact]
    public async Task GetUserByIdAsync_WithExistingUser_ReturnsUser()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: "testuser");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(1);
        result.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange: لا توجد مستخدمين في قاعدة البيانات

        // Act
        var result = await _userService.GetUserByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByIdAsync_WithInactiveUser_ReturnsUser()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: "testuser",
            isActive: false);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
    }

    #endregion

    #region GetUserByUsernameAsync Tests

    [Fact]
    public async Task GetUserByUsernameAsync_WithExistingUsername_ReturnsUser()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: "testuser");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserByUsernameAsync("testuser");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
        result.UserId.Should().Be(1);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_WithNonExistentUsername_ReturnsNull()
    {
        // Arrange: لا توجد مستخدمين في قاعدة البيانات

        // Act
        var result = await _userService.GetUserByUsernameAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByUsernameAsync_IsCaseSensitive()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: "TestUser");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserByUsernameAsync("testuser");

        // Assert
        result.Should().BeNull(); // Case-sensitive lookup
    }

    #endregion

    #region CreateUserAsync Tests

    [Fact]
    public async Task CreateUserAsync_WithValidData_CreatesUser()
    {
        // Arrange
        var newUser = TestDataBuilder.CreateUser(
            userId: 0, // Will be set by database
            username: "newuser",
            password: "NewPassword123",
            role: "admin",
            isActive: true);
        newUser.UserId = 0; // Reset to let database generate

        // Act
        var result = await _userService.CreateUserAsync(newUser);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().BeGreaterThan(0);
        result.Username.Should().Be("newuser");
        result.Role.Should().Be("admin");
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.PasswordHash.Should().NotBeNullOrEmpty();
        result.PasswordHash.Should().StartWith("$2"); // BCrypt hash format

        // Verify user was saved to database
        var savedUser = await _context.Users.FindAsync(result.UserId);
        savedUser.Should().NotBeNull();
        savedUser!.Username.Should().Be("newuser");

        // Verify Audit Log was called
        _mockAuditService.Verify(
            x => x.LogCreateAsync(
                "User",
                result.UserId,
                It.Is<string>(s => s.Contains("newuser"))),
            Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_WithPlainTextPassword_HashesPassword()
    {
        // Arrange
        var newUser = TestDataBuilder.CreateUser(
            userId: 0,
            username: "newuser",
            password: "PlainPassword123");
        newUser.PasswordHash = "PlainPassword123"; // Plain text password

        // Act
        var result = await _userService.CreateUserAsync(newUser);

        // Assert
        result.PasswordHash.Should().NotBe("PlainPassword123");
        result.PasswordHash.Should().StartWith("$2"); // BCrypt hash
        BCrypt.Net.BCrypt.Verify("PlainPassword123", result.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task CreateUserAsync_WithAlreadyHashedPassword_KeepsHash()
    {
        // Arrange
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("TestPassword123");
        var newUser = TestDataBuilder.CreateUser(
            userId: 0,
            username: "newuser");
        newUser.PasswordHash = hashedPassword;

        // Act
        var result = await _userService.CreateUserAsync(newUser);

        // Assert
        result.PasswordHash.Should().Be(hashedPassword);
        BCrypt.Net.BCrypt.Verify("TestPassword123", result.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task CreateUserAsync_SetsIsActiveToTrue()
    {
        // Arrange
        var newUser = TestDataBuilder.CreateUser(
            userId: 0,
            username: "newuser");
        newUser.IsActive = false; // Set to false initially

        // Act
        var result = await _userService.CreateUserAsync(newUser);

        // Assert
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateUserAsync_SetsCreatedAt()
    {
        // Arrange
        var newUser = TestDataBuilder.CreateUser(
            userId: 0,
            username: "newuser");
        newUser.CreatedAt = DateTime.MinValue; // Reset to test

        // Act
        var result = await _userService.CreateUserAsync(newUser);

        // Assert
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region ValidatePasswordAsync Tests

    [Fact]
    public async Task ValidatePasswordAsync_WithValidCredentials_ReturnsTrue()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: "testuser",
            password: "TestPassword123");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.ValidatePasswordAsync("testuser", "TestPassword123");

        // Assert
        result.Should().BeTrue();

        // Verify LastLogin was updated
        var updatedUser = await _context.Users.FindAsync(1);
        updatedUser!.LastLogin.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        updatedUser.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public async Task ValidatePasswordAsync_WithInvalidPassword_ReturnsFalse()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: "testuser",
            password: "CorrectPassword123");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.ValidatePasswordAsync("testuser", "WrongPassword");

        // Assert
        result.Should().BeFalse();

        // Verify failed attempt was recorded
        var updatedUser = await _context.Users.FindAsync(1);
        updatedUser!.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task ValidatePasswordAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange: لا توجد مستخدمين في قاعدة البيانات

        // Act
        var result = await _userService.ValidatePasswordAsync("nonexistent", "AnyPassword");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePasswordAsync_WithInactiveUser_ReturnsFalse()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: "testuser",
            password: "TestPassword123",
            isActive: false);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.ValidatePasswordAsync("testuser", "TestPassword123");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePasswordAsync_WithLockedAccount_ReturnsFalse()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: "testuser",
            password: "TestPassword123");
        user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.ValidatePasswordAsync("testuser", "TestPassword123");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePasswordAsync_WithValidPassword_ResetsFailedAttempts()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: "testuser",
            password: "TestPassword123");
        user.FailedLoginAttempts = 3; // Previous failed attempts
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.ValidatePasswordAsync("testuser", "TestPassword123");

        // Assert
        result.Should().BeTrue();
        var updatedUser = await _context.Users.FindAsync(1);
        updatedUser!.FailedLoginAttempts.Should().Be(0);
        updatedUser.LockedUntil.Should().BeNull();
    }

    #endregion

    #region RecordFailedLoginAttemptAsync Tests

    [Fact]
    public async Task RecordFailedLoginAttemptAsync_IncrementsFailedAttempts()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: "testuser");
        user.FailedLoginAttempts = 2;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _userService.RecordFailedLoginAttemptAsync("testuser");

        // Assert
        var updatedUser = await _context.Users.FindAsync(1);
        updatedUser!.FailedLoginAttempts.Should().Be(3);
    }

    [Fact]
    public async Task RecordFailedLoginAttemptAsync_WhenThresholdReached_LocksAccount()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: "testuser");
        user.FailedLoginAttempts = 4; // One less than threshold (5)
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _userService.RecordFailedLoginAttemptAsync("testuser");

        // Assert
        var updatedUser = await _context.Users.FindAsync(1);
        updatedUser!.FailedLoginAttempts.Should().Be(5);
        updatedUser.LockedUntil.Should().NotBeNull();
        updatedUser.LockedUntil.Should().BeCloseTo(
            DateTime.UtcNow.AddMinutes(15),
            TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task RecordFailedLoginAttemptAsync_WithNonExistentUser_DoesNotThrow()
    {
        // Arrange: لا توجد مستخدمين في قاعدة البيانات

        // Act & Assert
        var act = async () => await _userService.RecordFailedLoginAttemptAsync("nonexistent");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RecordFailedLoginAttemptAsync_DoesNotLockBeforeThreshold()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: "testuser");
        user.FailedLoginAttempts = 3; // Less than threshold (5)
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _userService.RecordFailedLoginAttemptAsync("testuser");

        // Assert
        var updatedUser = await _context.Users.FindAsync(1);
        updatedUser!.FailedLoginAttempts.Should().Be(4);
        updatedUser.LockedUntil.Should().BeNull();
    }

    #endregion

    #region ResetFailedLoginAttemptsAsync Tests

    [Fact]
    public async Task ResetFailedLoginAttemptsAsync_ResetsFailedAttempts()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: "testuser");
        user.FailedLoginAttempts = 5;
        user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _userService.ResetFailedLoginAttemptsAsync(1);

        // Assert
        var updatedUser = await _context.Users.FindAsync(1);
        updatedUser!.FailedLoginAttempts.Should().Be(0);
        updatedUser.LockedUntil.Should().BeNull();
    }

    [Fact]
    public async Task ResetFailedLoginAttemptsAsync_WithNonExistentUserId_DoesNotThrow()
    {
        // Arrange: لا توجد مستخدمين في قاعدة البيانات

        // Act & Assert
        var act = async () => await _userService.ResetFailedLoginAttemptsAsync(999);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ResetFailedLoginAttemptsAsync_WithExistingUser_DoesNotAffectOtherUsers()
    {
        // Arrange
        var user1 = TestDataBuilder.CreateUser(
            userId: 1,
            username: "user1");
        user1.FailedLoginAttempts = 3;

        var user2 = TestDataBuilder.CreateUser(
            userId: 2,
            username: "user2");
        user2.FailedLoginAttempts = 5;

        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        // Act
        await _userService.ResetFailedLoginAttemptsAsync(1);

        // Assert
        var updatedUser1 = await _context.Users.FindAsync(1);
        updatedUser1!.FailedLoginAttempts.Should().Be(0);

        var updatedUser2 = await _context.Users.FindAsync(2);
        updatedUser2!.FailedLoginAttempts.Should().Be(5); // Unchanged
    }

    #endregion

    #region IsAccountLockedAsync Tests

    [Fact]
    public async Task IsAccountLockedAsync_WithLockedAccount_ReturnsTrue()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: "testuser");
        user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.IsAccountLockedAsync("testuser");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAccountLockedAsync_WithUnlockedAccount_ReturnsFalse()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: "testuser");
        user.LockedUntil = null;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.IsAccountLockedAsync("testuser");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAccountLockedAsync_WithExpiredLockout_AutoUnlocksAccount()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: "testuser");
        user.LockedUntil = DateTime.UtcNow.AddMinutes(-5); // Expired lockout
        user.FailedLoginAttempts = 5;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.IsAccountLockedAsync("testuser");

        // Assert
        result.Should().BeFalse();
        var updatedUser = await _context.Users.FindAsync(1);
        updatedUser!.LockedUntil.Should().BeNull();
        updatedUser.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public async Task IsAccountLockedAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange: لا توجد مستخدمين في قاعدة البيانات

        // Act
        var result = await _userService.IsAccountLockedAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAccountLockedAsync_WithFutureLockout_ReturnsTrue()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: "testuser");
        user.LockedUntil = DateTime.UtcNow.AddMinutes(10);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.IsAccountLockedAsync("testuser");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region GetLockoutExpirationAsync Tests

    [Fact]
    public async Task GetLockoutExpirationAsync_WithLockedAccount_ReturnsExpirationTime()
    {
        // Arrange
        var lockoutExpiration = DateTime.UtcNow.AddMinutes(15);
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: "testuser");
        user.LockedUntil = lockoutExpiration;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetLockoutExpirationAsync("testuser");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeCloseTo(lockoutExpiration, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetLockoutExpirationAsync_WithUnlockedAccount_ReturnsNull()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(
            userId: 1,
            username: "testuser");
        user.LockedUntil = null;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetLockoutExpirationAsync("testuser");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLockoutExpirationAsync_WithNonExistentUser_ReturnsNull()
    {
        // Arrange: لا توجد مستخدمين في قاعدة البيانات

        // Act
        var result = await _userService.GetLockoutExpirationAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion
}


