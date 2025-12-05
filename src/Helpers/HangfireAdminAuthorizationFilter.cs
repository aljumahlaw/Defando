using Hangfire.Dashboard;

namespace Defando.Helpers;

/// <summary>
/// Authorization filter for Hangfire Dashboard that restricts access to Admin users only.
/// </summary>
public class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    /// <summary>
    /// Authorizes access to Hangfire Dashboard.
    /// Only authenticated users with "admin" role are allowed.
    /// </summary>
    /// <param name="context">The dashboard context containing HTTP context information.</param>
    /// <returns>true if the user is authenticated and has admin role; otherwise, false.</returns>
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // If HttpContext is null, deny access
        if (httpContext == null)
        {
            return false;
        }

        var user = httpContext.User;

        // Check if user is authenticated
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        // Check if user has "admin" role
        if (!user.IsInRole("admin"))
        {
            return false;
        }

        // User is authenticated and has admin role - allow access
        return true;
    }
}

