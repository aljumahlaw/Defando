using LegalDocSystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using BCrypt.Net;

namespace LegalDocSystem.Services;

/// <summary>
/// Service implementation for authentication and authorization operations.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserService _userService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthService> _logger;
    private const string UserIdSessionKey = "UserId";
    private const string UsernameSessionKey = "Username";

    public AuthService(
        IUserService userService, 
        IHttpContextAccessor httpContextAccessor, 
        IAuditService auditService,
        ILogger<AuthService> logger)
    {
        _userService = userService;
        _httpContextAccessor = httpContextAccessor;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user with username and password.
    /// </summary>
    public async Task<User?> LoginAsync(string username, string password)
    {
        try
        {
            var user = await _userService.GetUserByUsernameAsync(username);
            
            if (user == null || !user.IsActive)
            {
                // Log failed login attempt (user not found or inactive)
                await LogFailedLoginAsync(username, "User not found or inactive");
                return null;
            }

            // Check if account is locked
            if (await _userService.IsAccountLockedAsync(username))
            {
                // Log account lockout attempt
                await LogAccountLockoutAttemptAsync(user.UserId, username);
                return null;
            }

            var isValid = await _userService.ValidatePasswordAsync(username, password);
            
            if (!isValid)
            {
                // Log failed login attempt (invalid password)
                await LogFailedLoginAsync(username, "Invalid password");
                // Failed login attempt is already recorded in ValidatePasswordAsync
                return null;
            }

            // Store user info in session
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session != null)
            {
                session.SetInt32(UserIdSessionKey, user.UserId);
                session.SetString(UsernameSessionKey, user.Username);
            }

            // Create claims for Cookie Authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.GivenName, user.FullName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            if (!string.IsNullOrEmpty(user.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30), // Match session timeout
                AllowRefresh = true
            };

            // Sign in user with Cookie Authentication
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);
            }

            // Update last_login timestamp in database
            user.LastLogin = DateTime.UtcNow;
            await _userService.UpdateUserAsync(user); // Save the LastLogin change

            // Log successful login
            await LogSuccessfulLoginAsync(user.UserId, user.Username);

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while logging in user {Username}", username);
            return null;
        }
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    public async Task LogoutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            // Get user info BEFORE clearing session
            var session = httpContext.Session;
            var userId = GetCurrentUserId();
            var username = session?.GetString(UsernameSessionKey);

            // Sign out from Cookie Authentication
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Clear session AFTER reading user info
            if (session != null)
            {
                session.Clear();
            }

            // Log logout (using data read before clearing)
            if (userId.HasValue && !string.IsNullOrEmpty(username))
            {
                await LogLogoutAsync(userId.Value, username);
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Checks if the current user is authenticated.
    /// </summary>
    public async Task<bool> IsAuthenticatedAsync()
    {
        var userId = GetCurrentUserId();
        return userId.HasValue;
    }

    /// <summary>
    /// Gets the currently authenticated user.
    /// </summary>
    public async Task<User?> GetCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return null;
        }

        return await _userService.GetUserByIdAsync(userId.Value);
    }

    /// <summary>
    /// Gets the current user ID from session.
    /// </summary>
    public int? GetCurrentUserId()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null)
        {
            return null;
        }

        return session.GetInt32(UserIdSessionKey);
    }

    /// <summary>
    /// Attempts to login and returns detailed result including lockout status.
    /// </summary>
    public async Task<LoginResult> LoginWithResultAsync(string username, string password)
    {
        var result = new LoginResult();

        try
        {
            var user = await _userService.GetUserByUsernameAsync(username);
            
            if (user == null || !user.IsActive)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة.";
                return result;
            }

            // Check if account is locked
            if (await _userService.IsAccountLockedAsync(username))
            {
                result.IsSuccess = false;
                result.IsAccountLocked = true;
                result.LockoutExpiration = await _userService.GetLockoutExpirationAsync(username);
                
                if (result.LockoutExpiration.HasValue)
                {
                    var remainingMinutes = (int)Math.Ceiling((result.LockoutExpiration.Value - DateTime.UtcNow).TotalMinutes);
                    result.ErrorMessage = $"تم قفل الحساب مؤقتاً بعد محاولات تسجيل دخول فاشلة متعددة. يرجى المحاولة بعد {remainingMinutes} دقيقة.";
                }
                else
                {
                    result.ErrorMessage = "تم قفل الحساب مؤقتاً. يرجى المحاولة لاحقاً.";
                }
                
                return result;
            }

            var isValid = await _userService.ValidatePasswordAsync(username, password);
            
            if (!isValid)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة.";
                return result;
            }

            // Store user info in session
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session != null)
            {
                session.SetInt32(UserIdSessionKey, user.UserId);
                session.SetString(UsernameSessionKey, user.Username);
            }

            // Create claims for Cookie Authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.GivenName, user.FullName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            if (!string.IsNullOrEmpty(user.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30),
                AllowRefresh = true
            };

            // Sign in user with Cookie Authentication
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);
            }

            // Log successful login
            await LogSuccessfulLoginAsync(user.UserId, user.Username);

            result.IsSuccess = true;
            result.User = user;
            return result;
        }
        catch (Exception)
        {
            result.IsSuccess = false;
            result.ErrorMessage = "حدث خطأ أثناء تسجيل الدخول. يرجى المحاولة مرة أخرى.";
            return result;
        }
    }

    #region Private Audit Logging Methods

    /// <summary>
    /// Logs a successful login event.
    /// </summary>
    private async Task LogSuccessfulLoginAsync(int userId, string username)
    {
        try
        {
            await _auditService.LogLoginAsync(
                userId: userId,
                username: username,
                success: true,
                additionalData: $"Login successful from IP: {GetClientIpAddress()}"
            );
        }
        catch (Exception)
        {
            // Don't throw - audit logging should not break the login process
        }
    }

    /// <summary>
    /// Logs a failed login attempt.
    /// </summary>
    private async Task LogFailedLoginAsync(string username, string reason)
    {
        try
        {
            await _auditService.LogLoginAsync(
                userId: null,
                username: username,
                success: false,
                additionalData: $"Login failed: {reason}, IP: {GetClientIpAddress()}"
            );
        }
        catch (Exception)
        {
            // Don't throw - audit logging should not break the login process
        }
    }

    /// <summary>
    /// Logs an account lockout attempt.
    /// </summary>
    private async Task LogAccountLockoutAttemptAsync(int userId, string username)
    {
        try
        {
            var entry = new AuditLogEntry
            {
                Event = "AccountLockout",
                Category = "Authentication",
                Action = "account_locked_attempt",
                SubjectIdentifier = userId,
                SubjectName = username,
                SubjectType = "User",
                Data = $"Account is locked. Login attempt blocked. IP: {GetClientIpAddress()}",
                Created = DateTime.UtcNow
            };

            await _auditService.LogEventAsync(entry);
        }
        catch (Exception)
        {
            // Don't throw - audit logging should not break the login process
        }
    }

    /// <summary>
    /// Logs a logout event.
    /// </summary>
    private async Task LogLogoutAsync(int userId, string username)
    {
        try
        {
            await _auditService.LogLogoutAsync(
                userId: userId,
                username: username
            );
        }
        catch (Exception)
        {
            // Don't throw - audit logging should not break the logout process
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

        // Try X-Forwarded-For header (for load balancers/proxies)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ip = forwardedFor.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(ip))
                return ip;
        }

        // Try X-Real-IP header
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        // Fallback to Connection.RemoteIpAddress
        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    #endregion
}

