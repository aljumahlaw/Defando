using Defando.Data;
using Defando.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using BCrypt.Net;

namespace Defando.Services;

/// <summary>
/// Service implementation for user management operations.
/// </summary>
public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly int _maxFailedAttempts;
    private readonly int _lockoutDurationMinutes;
    private readonly bool _enableAutoUnlock;

    public UserService(
        ApplicationDbContext context, 
        IAuditService auditService,
        IConfiguration configuration)
    {
        _context = context;
        _auditService = auditService;
        
        // Read Account Lockout settings from configuration
        _maxFailedAttempts = configuration.GetValue<int>("AccountLockout:MaxFailedAttempts", 5);
        _lockoutDurationMinutes = configuration.GetValue<int>("AccountLockout:LockoutDurationMinutes", 15);
        _enableAutoUnlock = configuration.GetValue<bool>("AccountLockout:EnableAutoUnlock", true);
    }

    /// <summary>
    /// Retrieves all users from the database.
    /// </summary>
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves a user by its unique identifier.
    /// </summary>
    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == id);
    }

    /// <summary>
    /// Retrieves a user by username.
    /// </summary>
    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    /// <summary>
    /// Creates a new user in the database.
    /// </summary>
    public async Task<User> CreateUserAsync(User user)
    {
        // Hash password before saving
        if (!string.IsNullOrEmpty(user.PasswordHash) && 
            !user.PasswordHash.StartsWith("$2"))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
        }

        user.CreatedAt = DateTime.UtcNow;
        user.IsActive = true;

        // TODO: Validate username uniqueness
        // TODO: Validate email format if provided

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Log user creation
        try
        {
            await _auditService.LogCreateAsync(
                entityType: "User",
                entityId: user.UserId,
                additionalData: $"Username: {user.Username}, FullName: {user.FullName}, Role: {user.Role}, Email: {user.Email ?? "N/A"}"
            );
        }
        catch (Exception)
        {
            // Don't throw - audit logging should not break user creation
        }

        return user;
    }

    /// <summary>
    /// Validates user credentials (username and password).
    /// </summary>
    public async Task<bool> ValidatePasswordAsync(string username, string password)
    {
        var user = await GetUserByUsernameAsync(username);
        
        if (user == null || !user.IsActive)
        {
            return false;
        }

        // Check if account is locked
        if (await IsAccountLockedAsync(username))
        {
            return false;
        }

        var isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        
        if (!isValid)
        {
            // Record failed login attempt
            await RecordFailedLoginAttemptAsync(username);
        }
        else
        {
            // Reset failed attempts on successful login
            await ResetFailedLoginAttemptsAsync(user.UserId);
            
            // Update last login timestamp
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return isValid;
    }

    /// <summary>
    /// Records a failed login attempt and locks the account if threshold is reached.
    /// </summary>
    public async Task RecordFailedLoginAttemptAsync(string username)
    {
        var user = await GetUserByUsernameAsync(username);
        if (user == null)
        {
            return;
        }

        user.FailedLoginAttempts++;

        // Lock account if threshold is reached (using configurable settings)
        if (user.FailedLoginAttempts >= _maxFailedAttempts)
        {
            user.LockedUntil = DateTime.UtcNow.AddMinutes(_lockoutDurationMinutes);
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Resets failed login attempts after successful login.
    /// </summary>
    public async Task ResetFailedLoginAttemptsAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Checks if an account is currently locked.
    /// </summary>
    public async Task<bool> IsAccountLockedAsync(string username)
    {
        var user = await GetUserByUsernameAsync(username);
        if (user == null)
        {
            return false;
        }

        // Check if account is locked and lockout period has not expired
        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
        {
            return true;
        }

        // If lockout period has expired and auto-unlock is enabled, unlock the account
        if (_enableAutoUnlock && user.LockedUntil.HasValue && user.LockedUntil.Value <= DateTime.UtcNow)
        {
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            await _context.SaveChangesAsync();
        }

        return false;
    }

    /// <summary>
    /// Gets the lockout expiration time for a user account.
    /// </summary>
    public async Task<DateTime?> GetLockoutExpirationAsync(string username)
    {
        var user = await GetUserByUsernameAsync(username);
        return user?.LockedUntil;
    }

    /// <summary>
    /// Updates an existing user in the database.
    /// </summary>
    public async Task UpdateUserAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}

