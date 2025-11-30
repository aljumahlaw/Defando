using LegalDocSystem.Data;
using LegalDocSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LegalDocSystem.Services;

/// <summary>
/// Service implementation for audit logging operations.
/// </summary>
public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Sanitizes audit log data to remove sensitive information (passwords, tokens, etc.).
    /// </summary>
    private string? SanitizeAuditData(string? data)
    {
        if (string.IsNullOrEmpty(data))
            return data;

        var sensitivePatterns = new[]
        {
            // Password patterns
            @"(?i)password['""\s]*[:=]\s*['""]?([^'""\s]+)",
            @"(?i)pwd['""\s]*[:=]\s*['""]?([^'""\s]+)",
            @"(?i)pass['""\s]*[:=]\s*['""]?([^'""\s]+)",
            
            // Token patterns
            @"(?i)token['""\s]*[:=]\s*['""]?([^'""\s]+)",
            @"(?i)api[_-]?key['""\s]*[:=]\s*['""]?([^'""\s]+)",
            
            // Secret patterns
            @"(?i)secret['""\s]*[:=]\s*['""]?([^'""\s]+)",
            @"(?i)secret[_-]?key['""\s]*[:=]\s*['""]?([^'""\s]+)",
            
            // Connection string patterns
            @"(?i)connection[_-]?string['""\s]*[:=]\s*['""]?([^'""\s]+)",
            @"(?i)connection[_-]?str['""\s]*[:=]\s*['""]?([^'""\s]+)",
            
            // Credit card patterns (if applicable)
            @"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b",
        };

        var sanitized = data;
        foreach (var pattern in sensitivePatterns)
        {
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized, 
                pattern, 
                "[REDACTED]",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return sanitized;
    }

    /// <summary>
    /// Logs an audit event asynchronously.
    /// </summary>
    public async Task LogEventAsync(AuditLogEntry entry)
    {
        try
        {
            // Sanitize data before logging to remove sensitive information
            entry.Data = SanitizeAuditData(entry.Data);

            // Extract user information from HttpContext if not provided
            if (entry.SubjectIdentifier == null || string.IsNullOrEmpty(entry.SubjectName))
            {
                ExtractUserInfo(entry);
            }

            // Extract IP address if not provided
            if (string.IsNullOrEmpty(entry.IpAddress))
            {
                entry.IpAddress = GetClientIpAddress();
            }

            // Extract User Agent if not provided
            if (string.IsNullOrEmpty(entry.UserAgent))
            {
                entry.UserAgent = GetUserAgent();
            }

            // Map AuditLogEntry to AuditLog entity
            var auditLog = new AuditLog
            {
                UserId = entry.SubjectIdentifier,
                Action = entry.Action,
                EntityType = entry.EntityType ?? entry.Category,
                EntityId = entry.EntityId,
                Details = FormatDetails(entry),
                IpAddress = entry.IpAddress,
                CreatedAt = entry.Created
            };

            // Save to database asynchronously
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log error but don't throw - audit logging should not break the application
            // In production, consider using a separate logging mechanism (e.g., Serilog)
            Console.WriteLine($"Error logging audit event: {ex.Message}");
        }
    }

    /// <summary>
    /// Logs a login event.
    /// </summary>
    public async Task LogLoginAsync(int? userId, string? username, bool success, string? additionalData = null)
    {
        var entry = new AuditLogEntry
        {
            Event = "Login",
            Category = "Authentication",
            Action = success ? "login_success" : "login_failed",
            SubjectIdentifier = userId,
            SubjectName = username,
            SubjectType = "User",
            Data = additionalData ?? (success ? "Login successful" : "Login failed"),
            Created = DateTime.UtcNow
        };

        await LogEventAsync(entry);
    }

    /// <summary>
    /// Logs a logout event.
    /// </summary>
    public async Task LogLogoutAsync(int? userId, string? username)
    {
        var entry = new AuditLogEntry
        {
            Event = "Logout",
            Category = "Authentication",
            Action = "logout",
            SubjectIdentifier = userId,
            SubjectName = username,
            SubjectType = "User",
            Data = "User logged out",
            Created = DateTime.UtcNow
        };

        await LogEventAsync(entry);
    }

    /// <summary>
    /// Logs a create event.
    /// </summary>
    public async Task LogCreateAsync(string entityType, int? entityId, string? additionalData = null)
    {
        var entry = new AuditLogEntry
        {
            Event = "Create",
            Category = entityType,
            Action = $"create_{entityType.ToLower()}",
            EntityType = entityType,
            EntityId = entityId,
            Data = additionalData ?? $"Created {entityType}",
            Created = DateTime.UtcNow
        };

        await LogEventAsync(entry);
    }

    /// <summary>
    /// Logs an update event.
    /// </summary>
    public async Task LogUpdateAsync(string entityType, int? entityId, string? additionalData = null)
    {
        var entry = new AuditLogEntry
        {
            Event = "Update",
            Category = entityType,
            Action = $"update_{entityType.ToLower()}",
            EntityType = entityType,
            EntityId = entityId,
            Data = additionalData ?? $"Updated {entityType}",
            Created = DateTime.UtcNow
        };

        await LogEventAsync(entry);
    }

    /// <summary>
    /// Logs a delete event.
    /// </summary>
    public async Task LogDeleteAsync(string entityType, int? entityId, string? additionalData = null)
    {
        var entry = new AuditLogEntry
        {
            Event = "Delete",
            Category = entityType,
            Action = $"delete_{entityType.ToLower()}",
            EntityType = entityType,
            EntityId = entityId,
            Data = additionalData ?? $"Deleted {entityType}",
            Created = DateTime.UtcNow
        };

        await LogEventAsync(entry);
    }

    /// <summary>
    /// Retrieves audit logs based on specified criteria.
    /// </summary>
    public async Task<IEnumerable<AuditLog>> GetLogsAsync(
        int? userId = null,
        string? action = null,
        string? entityType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int skip = 0,
        int take = 100)
    {
        var query = _context.AuditLogs.AsQueryable();

        // Apply filters
        if (userId.HasValue)
        {
            query = query.Where(a => a.UserId == userId.Value);
        }

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(a => a.Action == action);
        }

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= endDate.Value);
        }

        // Order by creation date (newest first)
        query = query.OrderByDescending(a => a.CreatedAt);

        // Apply pagination
        query = query.Skip(skip).Take(take);

        return await query
            .Include(a => a.User)
            .ToListAsync();
    }

    #region Private Helper Methods

    /// <summary>
    /// Extracts user information from HttpContext Claims.
    /// </summary>
    private void ExtractUserInfo(AuditLogEntry entry)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var user = httpContext.User;

            // Extract User ID from NameIdentifier claim
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                entry.SubjectIdentifier = userId;
            }

            // Extract Username from Name claim
            var usernameClaim = user.FindFirst(ClaimTypes.Name);
            if (usernameClaim != null)
            {
                entry.SubjectName = usernameClaim.Value;
            }

            // Extract Role from Role claim
            var roleClaim = user.FindFirst(ClaimTypes.Role);
            if (roleClaim != null)
            {
                entry.SubjectType = roleClaim.Value;
            }
        }
    }

    /// <summary>
    /// Gets the client IP address from HttpContext.
    /// </summary>
    private string? GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return null;

        // Try to get IP from X-Forwarded-For header (for load balancers/proxies)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs, take the first one
            var ip = forwardedFor.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(ip))
                return ip;
        }

        // Try to get IP from X-Real-IP header
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        // Fallback to Connection.RemoteIpAddress
        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Gets the User Agent from HttpContext.
    /// </summary>
    private string? GetUserAgent()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.Request.Headers["User-Agent"].FirstOrDefault();
    }

    /// <summary>
    /// Formats audit log entry details into a single string for storage.
    /// </summary>
    private string? FormatDetails(AuditLogEntry entry)
    {
        var details = new List<string>();

        if (!string.IsNullOrEmpty(entry.Event))
            details.Add($"Event: {entry.Event}");

        if (!string.IsNullOrEmpty(entry.Category))
            details.Add($"Category: {entry.Category}");

        if (!string.IsNullOrEmpty(entry.SubjectName))
            details.Add($"Subject: {entry.SubjectName}");

        if (!string.IsNullOrEmpty(entry.SubjectType))
            details.Add($"SubjectType: {entry.SubjectType}");

        if (!string.IsNullOrEmpty(entry.Data))
            details.Add($"Data: {entry.Data}");

        if (!string.IsNullOrEmpty(entry.UserAgent))
            details.Add($"UserAgent: {entry.UserAgent}");

        return details.Count > 0 ? string.Join(" | ", details) : null;
    }

    #endregion
}

