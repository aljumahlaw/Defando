using FluentAssertions;
using LegalDocSystem.Data;
using LegalDocSystem.Models;
using LegalDocSystem.Services;
using LegalDocSystem.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Xunit;

namespace LegalDocSystem.Tests.Services;

/// <summary>
/// Unit tests for AuditService.
/// Tests audit logging, retrieval, and filtering functionality.
/// </summary>
public class AuditServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly AuditService _auditService;

    public AuditServiceTests()
    {
        // Setup In-Memory Database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Setup HttpContextAccessor Mock
        _mockHttpContextAccessor = MockHttpContextAccessor.CreateUnauthenticatedHttpContext();

        // Create AuditService instance
        _auditService = new AuditService(
            _context,
            _mockHttpContextAccessor.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region LogEventAsync Tests

    [Fact]
    public async Task LogEventAsync_WithValidEntry_SavesAuditLogToDatabase()
    {
        // Arrange: إعداد البيانات
        var entry = new AuditLogEntry
        {
            Event = "Test",
            Category = "TestCategory",
            Action = "test_action",
            SubjectIdentifier = 1,
            SubjectName = "testuser",
            SubjectType = "User",
            EntityType = "Document",
            EntityId = 100,
            Data = "Test data",
            Created = DateTime.UtcNow
        };

        // Act: تنفيذ الوظيفة المطلوب اختبارها
        await _auditService.LogEventAsync(entry);

        // Assert: التحقق من النتائج
        var savedLog = await _context.AuditLogs.FirstOrDefaultAsync();
        savedLog.Should().NotBeNull();
        savedLog!.Action.Should().Be("test_action");
        savedLog.EntityType.Should().Be("Document");
        savedLog.EntityId.Should().Be(100);
        savedLog.UserId.Should().Be(1);
        savedLog.Details.Should().Contain("testuser");
        savedLog.Details.Should().Contain("Test data");
    }

    [Fact]
    public async Task LogEventAsync_WithSystemAction_SavesWithNullUserId()
    {
        // Arrange
        var entry = new AuditLogEntry
        {
            Event = "System",
            Category = "System",
            Action = "system_action",
            SubjectIdentifier = null,
            SubjectName = "System",
            SubjectType = "System",
            EntityType = "System",
            EntityId = null,
            Data = "System action",
            Created = DateTime.UtcNow
        };

        // Act
        await _auditService.LogEventAsync(entry);

        // Assert
        var savedLog = await _context.AuditLogs.FirstOrDefaultAsync();
        savedLog.Should().NotBeNull();
        savedLog!.UserId.Should().BeNull();
        savedLog.Action.Should().Be("system_action");
        savedLog.Details.Should().Contain("System");
    }

    [Fact]
    public async Task LogEventAsync_WithNonExistentEntity_SavesWithCorrectEntityId()
    {
        // Arrange
        var entry = new AuditLogEntry
        {
            Event = "Delete",
            Category = "Document",
            Action = "delete_document",
            SubjectIdentifier = 1,
            SubjectName = "testuser",
            EntityType = "Document",
            EntityId = 999, // Non-existent entity
            Data = "Deleted document",
            Created = DateTime.UtcNow
        };

        // Act
        await _auditService.LogEventAsync(entry);

        // Assert
        var savedLog = await _context.AuditLogs.FirstOrDefaultAsync();
        savedLog.Should().NotBeNull();
        savedLog!.EntityId.Should().Be(999);
        savedLog.EntityType.Should().Be("Document");
    }

    [Fact]
    public async Task LogEventAsync_WithSensitiveData_SanitizesData()
    {
        // Arrange
        var entry = new AuditLogEntry
        {
            Event = "Login",
            Category = "Authentication",
            Action = "login",
            SubjectIdentifier = 1,
            SubjectName = "testuser",
            Data = "Password: MySecretPassword123",
            Created = DateTime.UtcNow
        };

        // Act
        await _auditService.LogEventAsync(entry);

        // Assert
        var savedLog = await _context.AuditLogs.FirstOrDefaultAsync();
        savedLog.Should().NotBeNull();
        savedLog!.Details.Should().NotContain("MySecretPassword123");
        savedLog.Details.Should().Contain("[REDACTED]");
    }

    [Fact]
    public async Task LogEventAsync_WithHttpContext_ExtractsUserInfo()
    {
        // Arrange
        var mockHttpContext = MockHttpContextAccessor.CreateMockHttpContext(
            userId: 2,
            username: "contextuser",
            role: "admin");

        var service = new AuditService(_context, mockHttpContext.Object);

        var entry = new AuditLogEntry
        {
            Event = "Test",
            Category = "Test",
            Action = "test_action",
            SubjectIdentifier = null, // Will be extracted from HttpContext
            SubjectName = null, // Will be extracted from HttpContext
            Created = DateTime.UtcNow
        };

        // Act
        await service.LogEventAsync(entry);

        // Assert
        var savedLog = await _context.AuditLogs.FirstOrDefaultAsync();
        savedLog.Should().NotBeNull();
        savedLog!.UserId.Should().Be(2);
        savedLog.Details.Should().Contain("contextuser");
    }

    #endregion

    #region LogCreateAsync Tests

    [Fact]
    public async Task LogCreateAsync_WithValidData_SavesCreateAuditLog()
    {
        // Arrange
        var entityType = "Document";
        var entityId = 1;
        var additionalData = "Created new document";

        // Act
        await _auditService.LogCreateAsync(entityType, entityId, additionalData);

        // Assert
        var savedLog = await _context.AuditLogs.FirstOrDefaultAsync();
        savedLog.Should().NotBeNull();
        savedLog!.Action.Should().Be("create_document");
        savedLog.EntityType.Should().Be("Document");
        savedLog.EntityId.Should().Be(1);
        savedLog.Details.Should().Contain("Created new document");
    }

    [Fact]
    public async Task LogCreateAsync_WithNullEntityId_SavesWithNullEntityId()
    {
        // Arrange
        var entityType = "Folder";
        int? entityId = null;

        // Act
        await _auditService.LogCreateAsync(entityType, entityId);

        // Assert
        var savedLog = await _context.AuditLogs.FirstOrDefaultAsync();
        savedLog.Should().NotBeNull();
        savedLog!.Action.Should().Be("create_folder");
        savedLog.EntityId.Should().BeNull();
    }

    #endregion

    #region LogUpdateAsync Tests

    [Fact]
    public async Task LogUpdateAsync_WithValidData_SavesUpdateAuditLog()
    {
        // Arrange
        var entityType = "Document";
        var entityId = 1;
        var additionalData = "Updated document name";

        // Act
        await _auditService.LogUpdateAsync(entityType, entityId, additionalData);

        // Assert
        var savedLog = await _context.AuditLogs.FirstOrDefaultAsync();
        savedLog.Should().NotBeNull();
        savedLog!.Action.Should().Be("update_document");
        savedLog.EntityType.Should().Be("Document");
        savedLog.EntityId.Should().Be(1);
        savedLog.Details.Should().Contain("Updated document name");
    }

    #endregion

    #region LogDeleteAsync Tests

    [Fact]
    public async Task LogDeleteAsync_WithValidData_SavesDeleteAuditLog()
    {
        // Arrange
        var entityType = "Document";
        var entityId = 1;
        var additionalData = "Deleted document permanently";

        // Act
        await _auditService.LogDeleteAsync(entityType, entityId, additionalData);

        // Assert
        var savedLog = await _context.AuditLogs.FirstOrDefaultAsync();
        savedLog.Should().NotBeNull();
        savedLog!.Action.Should().Be("delete_document");
        savedLog.EntityType.Should().Be("Document");
        savedLog.EntityId.Should().Be(1);
        savedLog.Details.Should().Contain("Deleted document permanently");
    }

    #endregion

    #region GetLogsAsync Tests

    [Fact]
    public async Task GetLogsAsync_WithNoFilters_ReturnsAllLogs()
    {
        // Arrange
        var user1 = TestDataBuilder.CreateUser(userId: 1, username: "user1");
        var user2 = TestDataBuilder.CreateUser(userId: 2, username: "user2");

        var log1 = new AuditLog
        {
            UserId = 1,
            Action = "create_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };

        var log2 = new AuditLog
        {
            UserId = 2,
            Action = "update_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        var log3 = new AuditLog
        {
            UserId = 1,
            Action = "delete_folder",
            EntityType = "Folder",
            EntityId = 2,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(user1, user2);
        _context.AuditLogs.AddRange(log1, log2, log3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _auditService.GetLogsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        // Should be ordered by CreatedAt descending (newest first)
        result.First().Action.Should().Be("delete_folder");
        result.Last().Action.Should().Be("create_document");
    }

    [Fact]
    public async Task GetLogsAsync_WithUserIdFilter_ReturnsOnlyUserLogs()
    {
        // Arrange
        var user1 = TestDataBuilder.CreateUser(userId: 1, username: "user1");
        var user2 = TestDataBuilder.CreateUser(userId: 2, username: "user2");

        var log1 = new AuditLog
        {
            UserId = 1,
            Action = "create_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var log2 = new AuditLog
        {
            UserId = 2,
            Action = "update_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(user1, user2);
        _context.AuditLogs.AddRange(log1, log2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _auditService.GetLogsAsync(userId: 1);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().UserId.Should().Be(1);
        result.First().Action.Should().Be("create_document");
    }

    [Fact]
    public async Task GetLogsAsync_WithActionFilter_ReturnsOnlyMatchingActions()
    {
        // Arrange
        var log1 = new AuditLog
        {
            UserId = 1,
            Action = "create_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var log2 = new AuditLog
        {
            UserId = 1,
            Action = "update_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var log3 = new AuditLog
        {
            UserId = 1,
            Action = "delete_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.AddRange(log1, log2, log3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _auditService.GetLogsAsync(action: "create_document");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Action.Should().Be("create_document");
    }

    [Fact]
    public async Task GetLogsAsync_WithEntityTypeFilter_ReturnsOnlyMatchingEntityTypes()
    {
        // Arrange
        var log1 = new AuditLog
        {
            UserId = 1,
            Action = "create_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var log2 = new AuditLog
        {
            UserId = 1,
            Action = "create_folder",
            EntityType = "Folder",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.AddRange(log1, log2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _auditService.GetLogsAsync(entityType: "Document");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().EntityType.Should().Be("Document");
    }

    [Fact]
    public async Task GetLogsAsync_WithDateRangeFilter_ReturnsOnlyLogsInRange()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-2);
        var endDate = DateTime.UtcNow.AddDays(-1);

        var log1 = new AuditLog
        {
            UserId = 1,
            Action = "create_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-3) // Outside range
        };

        var log2 = new AuditLog
        {
            UserId = 1,
            Action = "update_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-1.5) // Inside range
        };

        var log3 = new AuditLog
        {
            UserId = 1,
            Action = "delete_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow // Outside range
        };

        _context.AuditLogs.AddRange(log1, log2, log3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _auditService.GetLogsAsync(
            startDate: startDate,
            endDate: endDate);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Action.Should().Be("update_document");
    }

    [Fact]
    public async Task GetLogsAsync_WithNoLogs_ReturnsEmptyList()
    {
        // Arrange: لا توجد سجلات في قاعدة البيانات

        // Act
        var result = await _auditService.GetLogsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLogsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = 1,
                Action = $"action_{i}",
                EntityType = "Document",
                EntityId = i,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _auditService.GetLogsAsync(skip: 5, take: 5);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5);
        // Should be ordered by CreatedAt descending, so skip 5 means we get items 6-10
        result.First().Action.Should().Be("action_10");
        result.Last().Action.Should().Be("action_6");
    }

    [Fact]
    public async Task GetLogsAsync_WithMultipleFilters_ReturnsMatchingLogs()
    {
        // Arrange
        var user1 = TestDataBuilder.CreateUser(userId: 1, username: "user1");
        var user2 = TestDataBuilder.CreateUser(userId: 2, username: "user2");

        var log1 = new AuditLog
        {
            UserId = 1,
            Action = "create_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var log2 = new AuditLog
        {
            UserId = 1,
            Action = "update_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var log3 = new AuditLog
        {
            UserId = 2,
            Action = "create_document",
            EntityType = "Document",
            EntityId = 2,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(user1, user2);
        _context.AuditLogs.AddRange(log1, log2, log3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _auditService.GetLogsAsync(
            userId: 1,
            action: "create_document",
            entityType: "Document");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().UserId.Should().Be(1);
        result.First().Action.Should().Be("create_document");
        result.First().EntityType.Should().Be("Document");
    }

    #endregion

    #region LogLoginAsync Tests

    [Fact]
    public async Task LogLoginAsync_WithSuccessfulLogin_SavesLoginSuccessLog()
    {
        // Arrange
        var userId = 1;
        var username = "testuser";
        var success = true;

        // Act
        await _auditService.LogLoginAsync(userId, username, success);

        // Assert
        var savedLog = await _context.AuditLogs.FirstOrDefaultAsync();
        savedLog.Should().NotBeNull();
        savedLog!.Action.Should().Be("login_success");
        savedLog.UserId.Should().Be(1);
        savedLog.Details.Should().Contain("testuser");
        savedLog.Details.Should().Contain("Login successful");
    }

    [Fact]
    public async Task LogLoginAsync_WithFailedLogin_SavesLoginFailedLog()
    {
        // Arrange
        var userId = (int?)null;
        var username = "testuser";
        var success = false;
        var additionalData = "Invalid password";

        // Act
        await _auditService.LogLoginAsync(userId, username, success, additionalData);

        // Assert
        var savedLog = await _context.AuditLogs.FirstOrDefaultAsync();
        savedLog.Should().NotBeNull();
        savedLog!.Action.Should().Be("login_failed");
        savedLog.UserId.Should().BeNull();
        savedLog.Details.Should().Contain("testuser");
        savedLog.Details.Should().Contain("Invalid password");
    }

    #endregion

    #region LogLogoutAsync Tests

    [Fact]
    public async Task LogLogoutAsync_WithValidUser_SavesLogoutLog()
    {
        // Arrange
        var userId = 1;
        var username = "testuser";

        // Act
        await _auditService.LogLogoutAsync(userId, username);

        // Assert
        var savedLog = await _context.AuditLogs.FirstOrDefaultAsync();
        savedLog.Should().NotBeNull();
        savedLog!.Action.Should().Be("logout");
        savedLog.UserId.Should().Be(1);
        savedLog.Details.Should().Contain("testuser");
        savedLog.Details.Should().Contain("User logged out");
    }

    #endregion

    #region GetLogsAsync - Additional Tests (GetAuditTrailAsync equivalent)

    [Fact]
    public async Task GetLogsAsync_WithEntityTypeAndEntityId_ReturnsEntityAuditTrail()
    {
        // Arrange
        var log1 = new AuditLog
        {
            UserId = 1,
            Action = "create_document",
            EntityType = "Document",
            EntityId = 100,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };

        var log2 = new AuditLog
        {
            UserId = 1,
            Action = "update_document",
            EntityType = "Document",
            EntityId = 100,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        var log3 = new AuditLog
        {
            UserId = 2,
            Action = "create_document",
            EntityType = "Document",
            EntityId = 200, // Different entity
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.AddRange(log1, log2, log3);
        await _context.SaveChangesAsync();

        // Act
        // Get audit trail for specific entity (EntityId = 100)
        var result = await _auditService.GetLogsAsync(
            entityType: "Document",
            startDate: null,
            endDate: null);

        // Filter by EntityId manually (since GetLogsAsync doesn't support EntityId filter directly)
        var entityTrail = result.Where(r => r.EntityId == 100).ToList();

        // Assert
        entityTrail.Should().NotBeNull();
        entityTrail.Should().HaveCount(2);
        entityTrail.Should().OnlyContain(r => r.EntityId == 100);
        entityTrail.Should().OnlyContain(r => r.EntityType == "Document");
        // Should be ordered by CreatedAt descending (newest first)
        entityTrail[0].Action.Should().Be("update_document");
        entityTrail[1].Action.Should().Be("create_document");
    }

    [Fact]
    public async Task GetLogsAsync_WithSpecificDate_ReturnsOnlyLogsForThatDate()
    {
        // Arrange
        var targetDate = DateTime.UtcNow.Date; // Today

        var log1 = new AuditLog
        {
            UserId = 1,
            Action = "create_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = targetDate.AddHours(10) // Today at 10 AM
        };

        var log2 = new AuditLog
        {
            UserId = 1,
            Action = "update_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = targetDate.AddHours(15) // Today at 3 PM
        };

        var log3 = new AuditLog
        {
            UserId = 1,
            Action = "delete_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = targetDate.AddDays(-1).AddHours(10) // Yesterday
        };

        _context.AuditLogs.AddRange(log1, log2, log3);
        await _context.SaveChangesAsync();

        // Act
        // Get logs for specific date (start and end of the same day)
        var result = await _auditService.GetLogsAsync(
            startDate: targetDate,
            endDate: targetDate.AddDays(1).AddTicks(-1)); // End of the same day

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.CreatedAt.Date == targetDate);
        result.Should().Contain(r => r.Action == "create_document");
        result.Should().Contain(r => r.Action == "update_document");
        result.Should().NotContain(r => r.Action == "delete_document");
    }

    #endregion

    #region GetLogsAsync - Action Filter Tests (GetAuditTrailByActionAsync equivalent)

    [Fact]
    public async Task GetLogsAsync_WithCreateAction_ReturnsOnlyCreateActions()
    {
        // Arrange
        var log1 = new AuditLog
        {
            UserId = 1,
            Action = "create_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var log2 = new AuditLog
        {
            UserId = 1,
            Action = "update_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var log3 = new AuditLog
        {
            UserId = 1,
            Action = "create_folder",
            EntityType = "Folder",
            EntityId = 2,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.AddRange(log1, log2, log3);
        await _context.SaveChangesAsync();

        // Act
        // Get audit trail for Create actions only
        var result = await _auditService.GetLogsAsync(action: "create_document");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Action.Should().Be("create_document");
        result.First().EntityType.Should().Be("Document");
    }

    [Fact]
    public async Task GetLogsAsync_WithUpdateAction_ReturnsOnlyUpdateActions()
    {
        // Arrange
        var log1 = new AuditLog
        {
            UserId = 1,
            Action = "create_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var log2 = new AuditLog
        {
            UserId = 1,
            Action = "update_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var log3 = new AuditLog
        {
            UserId = 1,
            Action = "delete_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.AddRange(log1, log2, log3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _auditService.GetLogsAsync(action: "update_document");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Action.Should().Be("update_document");
    }

    [Fact]
    public async Task GetLogsAsync_WithDeleteAction_ReturnsOnlyDeleteActions()
    {
        // Arrange
        var log1 = new AuditLog
        {
            UserId = 1,
            Action = "create_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var log2 = new AuditLog
        {
            UserId = 1,
            Action = "delete_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.AddRange(log1, log2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _auditService.GetLogsAsync(action: "delete_document");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Action.Should().Be("delete_document");
    }

    [Fact]
    public async Task GetLogsAsync_WithNonExistentAction_ReturnsEmptyList()
    {
        // Arrange
        var log1 = new AuditLog
        {
            UserId = 1,
            Action = "create_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log1);
        await _context.SaveChangesAsync();

        // Act
        var result = await _auditService.GetLogsAsync(action: "nonexistent_action");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region PruneAuditLogsAsync Tests

    // NOTE: PruneAuditLogsAsync is not currently implemented in IAuditService.
    // This test section is prepared for when the functionality is added.
    // For now, we test the concept using direct database operations.

    [Fact]
    public async Task PruneAuditLogs_ConceptualTest_DeletesOldLogs()
    {
        // Arrange
        var oldDate = DateTime.UtcNow.AddDays(-100);
        var recentDate = DateTime.UtcNow.AddDays(-5);

        var oldLog1 = new AuditLog
        {
            UserId = 1,
            Action = "create_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = oldDate
        };

        var oldLog2 = new AuditLog
        {
            UserId = 1,
            Action = "update_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = oldDate.AddDays(-10)
        };

        var recentLog = new AuditLog
        {
            UserId = 1,
            Action = "delete_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = recentDate
        };

        _context.AuditLogs.AddRange(oldLog1, oldLog2, recentLog);
        await _context.SaveChangesAsync();

        // Act
        // NOTE: PruneAuditLogsAsync is not implemented, so we simulate the logic
        // In a real implementation, this would be: await _auditService.PruneAuditLogsAsync(retentionDays: 90);
        var cutoffDate = DateTime.UtcNow.AddDays(-90);
        var logsToDelete = await _context.AuditLogs
            .Where(a => a.CreatedAt < cutoffDate)
            .ToListAsync();
        
        _context.AuditLogs.RemoveRange(logsToDelete);
        await _context.SaveChangesAsync();

        // Assert
        var remainingLogs = await _context.AuditLogs.ToListAsync();
        remainingLogs.Should().HaveCount(1);
        remainingLogs.First().CreatedAt.Should().BeAfter(cutoffDate);
        remainingLogs.First().Action.Should().Be("delete_document");
    }

    [Fact]
    public async Task PruneAuditLogs_ConceptualTest_KeepsRecentLogs()
    {
        // Arrange
        var recentDate1 = DateTime.UtcNow.AddDays(-10);
        var recentDate2 = DateTime.UtcNow.AddDays(-5);
        var recentDate3 = DateTime.UtcNow.AddDays(-1);

        var recentLog1 = new AuditLog
        {
            UserId = 1,
            Action = "create_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = recentDate1
        };

        var recentLog2 = new AuditLog
        {
            UserId = 1,
            Action = "update_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = recentDate2
        };

        var recentLog3 = new AuditLog
        {
            UserId = 1,
            Action = "delete_document",
            EntityType = "Document",
            EntityId = 1,
            CreatedAt = recentDate3
        };

        _context.AuditLogs.AddRange(recentLog1, recentLog2, recentLog3);
        await _context.SaveChangesAsync();

        // Act
        // NOTE: PruneAuditLogsAsync is not implemented, so we simulate the logic
        var cutoffDate = DateTime.UtcNow.AddDays(-90);
        var logsToDelete = await _context.AuditLogs
            .Where(a => a.CreatedAt < cutoffDate)
            .ToListAsync();
        
        _context.AuditLogs.RemoveRange(logsToDelete);
        await _context.SaveChangesAsync();

        // Assert
        var remainingLogs = await _context.AuditLogs.ToListAsync();
        remainingLogs.Should().HaveCount(3); // All logs are recent, none should be deleted
        remainingLogs.Should().OnlyContain(r => r.CreatedAt >= cutoffDate);
    }

    #endregion
}

