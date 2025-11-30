using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System.Threading.RateLimiting;

namespace LegalDocSystem.Middleware;

/// <summary>
/// Middleware to apply rate limiting specifically to login endpoint.
/// Prevents brute-force attacks by limiting login attempts per IP address.
/// </summary>
public class LoginRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoginRateLimitMiddleware> _logger;
    private readonly RateLimiter _rateLimiter;

    public LoginRateLimitMiddleware(
        RequestDelegate next, 
        ILogger<LoginRateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        
        // Create rate limiter: 5 requests per minute per IP
        _rateLimiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0 // No queuing for login attempts
        });
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply to login POST requests
        if (context.Request.Path.StartsWithSegments("/login") && 
            context.Request.Method == "POST")
        {
            var ipAddress = context.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
            var lease = await _rateLimiter.AcquireAsync(permitCount: 1);

            if (!lease.IsAcquired)
            {
                _logger.LogWarning($"Login rate limit exceeded for IP: {ipAddress}");
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.RetryAfter = "60"; // Retry after 60 seconds
                await context.Response.WriteAsync("Too many login attempts. Please try again after 1 minute.");
                return;
            }

            try
            {
                await _next(context);
            }
            finally
            {
                lease.Dispose();
            }
        }
        else
        {
            await _next(context);
        }
    }
}

