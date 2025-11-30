using LegalDocSystem.Models;

namespace LegalDocSystem.Services;

/// <summary>
/// Service interface for user management operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Retrieves all users from the database.
    /// </summary>
    Task<List<User>> GetAllUsersAsync();

    /// <summary>
    /// Retrieves a user by its unique identifier.
    /// </summary>
    Task<User?> GetUserByIdAsync(int id);

    /// <summary>
    /// Retrieves a user by username.
    /// </summary>
    Task<User?> GetUserByUsernameAsync(string username);

    /// <summary>
    /// Creates a new user in the database.
    /// </summary>
    Task<User> CreateUserAsync(User user);

    /// <summary>
    /// Validates user credentials (username and password).
    /// </summary>
    Task<bool> ValidatePasswordAsync(string username, string password);

    /// <summary>
    /// Records a failed login attempt and locks the account if threshold is reached.
    /// </summary>
    Task RecordFailedLoginAttemptAsync(string username);

    /// <summary>
    /// Resets failed login attempts after successful login.
    /// </summary>
    Task ResetFailedLoginAttemptsAsync(int userId);

    /// <summary>
    /// Checks if an account is currently locked.
    /// </summary>
    Task<bool> IsAccountLockedAsync(string username);

    /// <summary>
    /// Gets the lockout expiration time for a user account.
    /// </summary>
    Task<DateTime?> GetLockoutExpirationAsync(string username);
}

