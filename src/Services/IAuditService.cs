using LegalDocSystem.Models;

namespace LegalDocSystem.Services;

/// <summary>
/// Service interface for audit logging operations.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Logs an audit event asynchronously.
    /// </summary>
    /// <param name="entry">The audit log entry to record.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogEventAsync(AuditLogEntry entry);

    /// <summary>
    /// Logs a login event.
    /// </summary>
    /// <param name="userId">The ID of the user who logged in.</param>
    /// <param name="username">The username of the user.</param>
    /// <param name="success">Whether the login was successful.</param>
    /// <param name="additionalData">Additional data to include in the log entry.</param>
    Task LogLoginAsync(int? userId, string? username, bool success, string? additionalData = null);

    /// <summary>
    /// Logs a logout event.
    /// </summary>
    /// <param name="userId">The ID of the user who logged out.</param>
    /// <param name="username">The username of the user.</param>
    Task LogLogoutAsync(int? userId, string? username);

    /// <summary>
    /// Logs a create event.
    /// </summary>
    /// <param name="entityType">The type of entity created (e.g., "Document", "Folder").</param>
    /// <param name="entityId">The ID of the created entity.</param>
    /// <param name="additionalData">Additional data about the creation.</param>
    Task LogCreateAsync(string entityType, int? entityId, string? additionalData = null);

    /// <summary>
    /// Logs an update event.
    /// </summary>
    /// <param name="entityType">The type of entity updated (e.g., "Document", "Folder").</param>
    /// <param name="entityId">The ID of the updated entity.</param>
    /// <param name="additionalData">Additional data about the update (e.g., changed fields).</param>
    Task LogUpdateAsync(string entityType, int? entityId, string? additionalData = null);

    /// <summary>
    /// Logs a delete event.
    /// </summary>
    /// <param name="entityType">The type of entity deleted (e.g., "Document", "Folder").</param>
    /// <param name="entityId">The ID of the deleted entity.</param>
    /// <param name="additionalData">Additional data about the deletion.</param>
    Task LogDeleteAsync(string entityType, int? entityId, string? additionalData = null);

    /// <summary>
    /// Retrieves audit logs based on specified criteria.
    /// </summary>
    /// <param name="userId">Filter by user ID (optional).</param>
    /// <param name="action">Filter by action (optional).</param>
    /// <param name="entityType">Filter by entity type (optional).</param>
    /// <param name="startDate">Start date for filtering (optional).</param>
    /// <param name="endDate">End date for filtering (optional).</param>
    /// <param name="skip">Number of records to skip for pagination.</param>
    /// <param name="take">Number of records to take for pagination.</param>
    /// <returns>A collection of audit log entries.</returns>
    Task<IEnumerable<AuditLog>> GetLogsAsync(
        int? userId = null,
        string? action = null,
        string? entityType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int skip = 0,
        int take = 100);
}

