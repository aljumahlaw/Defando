using LegalDocSystem.Models;
using BCrypt.Net;

namespace LegalDocSystem.Tests.Helpers;

/// <summary>
/// Builder pattern for creating test data entities.
/// Provides fluent API for building test objects with default values.
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// Creates a test user with default or custom values.
    /// </summary>
    /// <param name="userId">Optional user ID (default: 1).</param>
    /// <param name="username">Optional username (default: "testuser").</param>
    /// <param name="password">Optional plain text password (default: "TestPassword123").</param>
    /// <param name="role">Optional role (default: "user").</param>
    /// <param name="isActive">Optional active status (default: true).</param>
    /// <returns>A User instance with specified or default values.</returns>
    public static User CreateUser(
        int? userId = null,
        string? username = null,
        string? password = null,
        string? role = null,
        bool? isActive = null)
    {
        var plainPassword = password ?? "TestPassword123";
        
        return new User
        {
            UserId = userId ?? 1,
            Username = username ?? "testuser",
            PasswordHash = BCrypt.HashPassword(plainPassword),
            FullName = "Test User",
            Email = "test@example.com",
            Role = role ?? "user",
            IsActive = isActive ?? true,
            CreatedAt = DateTime.UtcNow,
            FailedLoginAttempts = 0,
            LockedUntil = null
        };
    }

    /// <summary>
    /// Creates a test document with default or custom values.
    /// </summary>
    /// <param name="documentId">Optional document ID (default: 1).</param>
    /// <param name="folderId">Optional folder ID (default: null).</param>
    /// <param name="uploadedBy">Optional user ID who uploaded (default: 1).</param>
    /// <returns>A Document instance with specified or default values.</returns>
    public static Document CreateDocument(
        int? documentId = null,
        int? folderId = null,
        int? uploadedBy = null)
    {
        return new Document
        {
            DocumentId = documentId ?? 1,
            DocumentName = "Test Document",
            DocumentType = "contract",
            FolderId = folderId,
            UploadedBy = uploadedBy ?? 1,
            UploadedAt = DateTime.UtcNow,
            FileGuid = Guid.NewGuid().ToString(),
            FilePath = "/test/path/document.pdf",
            FileSize = 1024,
            IsLocked = false,
            LockedBy = null,
            LockedAt = null
        };
    }

    /// <summary>
    /// Creates a test folder with default or custom values.
    /// </summary>
    /// <param name="folderId">Optional folder ID (default: 1).</param>
    /// <param name="parentId">Optional parent folder ID (default: null).</param>
    /// <returns>A Folder instance with specified or default values.</returns>
    public static Folder CreateFolder(
        int? folderId = null,
        int? parentId = null)
    {
        return new Folder
        {
            FolderId = folderId ?? 1,
            FolderName = "Test Folder",
            ParentId = parentId,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test task with default or custom values.
    /// </summary>
    /// <param name="taskId">Optional task ID (default: 1).</param>
    /// <param name="assignedTo">Optional user ID assigned to (default: 1).</param>
    /// <returns>A TaskItem instance with specified or default values.</returns>
    public static TaskItem CreateTask(
        int? taskId = null,
        int? assignedTo = null)
    {
        return new TaskItem
        {
            TaskId = taskId ?? 1,
            TaskTitle = "Test Task",
            TaskDescription = "Test Description",
            Status = "pending",
            AssignedTo = assignedTo ?? 1,
            AssignedBy = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}

