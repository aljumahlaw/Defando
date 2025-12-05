using Defando.Models;
using Defando.Services;
using System.Diagnostics;
using System.Security.Claims;

namespace Defando.Middleware;

/// <summary>
/// Middleware for automatically logging all HTTP requests to the audit log.
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        // Skip logging for certain paths (static files, health checks, etc.)
        if (ShouldSkipLogging(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;

        // Extract request information
        var requestMethod = context.Request.Method;
        var requestPath = context.Request.Path.Value ?? string.Empty;
        var queryString = context.Request.QueryString.Value ?? string.Empty;
        var fullUrl = $"{requestPath}{queryString}";

        // Extract user information
        var userId = GetUserId(context);
        var username = GetUsername(context);
        var userRole = GetUserRole(context);

        // Extract IP address
        var ipAddress = GetClientIpAddress(context);

        // Extract User Agent
        var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";

        // Determine request category
        var category = DetermineCategory(requestPath);

        try
        {
            // Log request start
            await LogRequestStartAsync(
                auditService,
                requestMethod,
                fullUrl,
                userId,
                username,
                userRole,
                ipAddress,
                userAgent,
                category
            );

            // Process the request
            await _next(context);

            stopwatch.Stop();

            // Log request completion (success)
            await LogRequestCompletionAsync(
                auditService,
                requestMethod,
                fullUrl,
                userId,
                username,
                userRole,
                ipAddress,
                userAgent,
                category,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                success: true
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log request completion (error)
            await LogRequestErrorAsync(
                auditService,
                requestMethod,
                fullUrl,
                userId,
                username,
                userRole,
                ipAddress,
                userAgent,
                category,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                ex
            );

            // Re-throw the exception to maintain the error handling flow
            throw;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Determines if the request path should be skipped from logging.
    /// </summary>
    private static bool ShouldSkipLogging(PathString path)
    {
        var pathValue = path.Value?.ToLower() ?? string.Empty;

        // Skip static files
        if (pathValue.StartsWith("/css/") ||
            pathValue.StartsWith("/js/") ||
            pathValue.StartsWith("/lib/") ||
            pathValue.StartsWith("/images/") ||
            pathValue.StartsWith("/fonts/") ||
            pathValue.StartsWith("/favicon.ico") ||
            pathValue.StartsWith("/_framework/") ||
            pathValue.StartsWith("/_content/"))
        {
            return true;
        }

        // Skip health checks and monitoring endpoints
        if (pathValue.StartsWith("/health") ||
            pathValue.StartsWith("/metrics"))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines the category of the request based on the path.
    /// </summary>
    private static string DetermineCategory(string path)
    {
        var pathLower = path.ToLower();

        if (pathLower.Contains("/api/documents") || pathLower.Contains("/documents"))
            return "Document";
        if (pathLower.Contains("/api/folders") || pathLower.Contains("/folders"))
            return "Folder";
        if (pathLower.Contains("/api/tasks") || pathLower.Contains("/tasks"))
            return "Task";
        if (pathLower.Contains("/api/users") || pathLower.Contains("/users"))
            return "User";
        if (pathLower.Contains("/api/shared") || pathLower.Contains("/shared"))
            return "SharedLink";
        if (pathLower.Contains("/login") || pathLower.Contains("/logout"))
            return "Authentication";
        if (pathLower.Contains("/api/outgoing") || pathLower.Contains("/outgoing"))
            return "Outgoing";
        if (pathLower.Contains("/api/incoming") || pathLower.Contains("/incoming"))
            return "Incoming";
        if (pathLower.Contains("/settings") || pathLower.Contains("/smtp"))
            return "Settings";
        if (pathLower.Contains("/hangfire"))
            return "System";
        if (pathLower.StartsWith("/api/"))
            return "API";

        return "General";
    }

    /// <summary>
    /// Gets the user ID from HttpContext.
    /// </summary>
    private static int? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Gets the username from HttpContext.
    /// </summary>
    private static string? GetUsername(HttpContext context)
    {
        return context.User?.FindFirst(ClaimTypes.Name)?.Value;
    }

    /// <summary>
    /// Gets the user role from HttpContext.
    /// </summary>
    private static string? GetUserRole(HttpContext context)
    {
        return context.User?.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Gets the client IP address from HttpContext.
    /// </summary>
    private static string? GetClientIpAddress(HttpContext context)
    {
        // Try X-Forwarded-For header (for load balancers/proxies)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ip = forwardedFor.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(ip))
                return ip;
        }

        // Try X-Real-IP header
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        // Fallback to Connection.RemoteIpAddress
        return context.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Logs the start of a request.
    /// </summary>
    private async Task LogRequestStartAsync(
        IAuditService auditService,
        string method,
        string url,
        int? userId,
        string? username,
        string? role,
        string? ipAddress,
        string? userAgent,
        string category)
    {
        try
        {
            var entry = new AuditLogEntry
            {
                Event = "HttpRequest",
                Category = category,
                Action = $"http_{method.ToLower()}_start",
                SubjectIdentifier = userId,
                SubjectName = username,
                SubjectType = role,
                Data = $"Request started: {method} {url} | UserAgent: {userAgent}",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Created = DateTime.UtcNow
            };

            await auditService.LogEventAsync(entry);
        }
        catch (Exception ex)
        {
            // Don't throw - audit logging should not break the request
            _logger.LogWarning(ex, "Failed to log request start");
        }
    }

    /// <summary>
    /// Logs the successful completion of a request.
    /// </summary>
    private async Task LogRequestCompletionAsync(
        IAuditService auditService,
        string method,
        string url,
        int? userId,
        string? username,
        string? role,
        string? ipAddress,
        string? userAgent,
        string category,
        int statusCode,
        long durationMs,
        bool success)
    {
        try
        {
            var statusText = success ? "Success" : "Failed";
            var entry = new AuditLogEntry
            {
                Event = "HttpRequest",
                Category = category,
                Action = $"http_{method.ToLower()}_completed",
                SubjectIdentifier = userId,
                SubjectName = username,
                SubjectType = role,
                Data = $"Request completed: {method} {url} | Status: {statusCode} ({statusText}) | Duration: {durationMs}ms",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Created = DateTime.UtcNow
            };

            await auditService.LogEventAsync(entry);
        }
        catch (Exception ex)
        {
            // Don't throw - audit logging should not break the request
            _logger.LogWarning(ex, "Failed to log request completion");
        }
    }

    /// <summary>
    /// Logs an error that occurred during request processing.
    /// </summary>
    private async Task LogRequestErrorAsync(
        IAuditService auditService,
        string method,
        string url,
        int? userId,
        string? username,
        string? role,
        string? ipAddress,
        string? userAgent,
        string category,
        int statusCode,
        long durationMs,
        Exception exception)
    {
        try
        {
            var entry = new AuditLogEntry
            {
                Event = "HttpRequest",
                Category = category,
                Action = $"http_{method.ToLower()}_error",
                SubjectIdentifier = userId,
                SubjectName = username,
                SubjectType = role,
                Data = $"Request error: {method} {url} | Status: {statusCode} | Duration: {durationMs}ms | Error: {exception.Message}",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Created = DateTime.UtcNow
            };

            await auditService.LogEventAsync(entry);
        }
        catch (Exception ex)
        {
            // Don't throw - audit logging should not break the request
            _logger.LogError(ex, "Failed to log request error");
        }
    }

    #endregion
}

