using Defando.Models;

namespace Defando.Services;

/// <summary>
/// Service interface for authentication and authorization operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user with username and password.
    /// </summary>
    Task<User?> LoginAsync(string username, string password);

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Checks if the current user is authenticated.
    /// </summary>
    Task<bool> IsAuthenticatedAsync();

    /// <summary>
    /// Gets the currently authenticated user.
    /// </summary>
    Task<User?> GetCurrentUserAsync();

    /// <summary>
    /// Gets the current user ID from session.
    /// </summary>
    int? GetCurrentUserId();

    /// <summary>
    /// Attempts to login and returns detailed result including lockout status.
    /// </summary>
    Task<LoginResult> LoginWithResultAsync(string username, string password);
}

/// <summary>
/// Result of login attempt with detailed information.
/// </summary>
public class LoginResult
{
    public bool IsSuccess { get; set; }
    public User? User { get; set; }
    public bool IsAccountLocked { get; set; }
    public DateTime? LockoutExpiration { get; set; }
    public string? ErrorMessage { get; set; }
}

