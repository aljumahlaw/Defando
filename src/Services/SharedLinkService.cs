using Defando.Data;
using Defando.Models;
using Defando.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace Defando.Services;

/// <summary>
/// Service implementation for managing shared links.
/// </summary>
public class SharedLinkService : ISharedLinkService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SharedLinkService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IAuditService _auditService;

    public SharedLinkService(
        ApplicationDbContext context, 
        ILogger<SharedLinkService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IAuditService auditService)
    {
        _context = context;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _auditService = auditService;
    }

    /// <summary>
    /// Creates a new shared link for a document.
    /// </summary>
    public async Task<SharedLink> CreateSharedLinkAsync(
        int documentId,
        int createdBy,
        DateTime expiresAt,
        int? maxAccessCount = null,
        string? password = null)
    {
        // Verify document exists
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null)
            throw new ArgumentException("Document not found", nameof(documentId));

        // Generate unique token
        var token = GenerateUniqueToken();

        // Hash password if provided
        string? passwordHash = null;
        if (!string.IsNullOrEmpty(password))
        {
            passwordHash = HashPassword(password);
        }

        var sharedLink = new SharedLink
        {
            DocumentId = documentId,
            LinkToken = token,
            CreatedBy = createdBy,
            ExpiresAt = expiresAt,
            MaxAccessCount = maxAccessCount,
            CurrentAccessCount = 0,
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.SharedLinks.Add(sharedLink);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Created shared link {sharedLink.LinkId} for document {documentId}");

        // Log shared link creation
        try
        {
            await _auditService.LogCreateAsync(
                entityType: "SharedLink",
                entityId: sharedLink.LinkId,
                additionalData: $"Document: {document.DocumentName} (ID: {documentId}), Expires: {expiresAt:yyyy-MM-dd HH:mm}, MaxAccess: {maxAccessCount?.ToString() ?? "Unlimited"}, PasswordProtected: {!string.IsNullOrEmpty(password)}"
            );
        }
        catch (Exception)
        {
            // Don't throw - audit logging should not break link creation
        }

        // Send notification email to the creator (if email notifications are enabled)
        try
        {
            var createdByUser = await _context.Users.FindAsync(createdBy);
            if (createdByUser != null && !string.IsNullOrEmpty(createdByUser.Email))
            {
                var emailService = _serviceProvider.GetRequiredService<IEmailService>();
                // Get base URL from configuration or use default
                var baseUrl = _configuration["EmailNotifications:BaseUrl"] 
                    ?? "https://yourdomain.com"; // Fallback if not configured
                var linkUrl = $"{baseUrl.TrimEnd('/')}/shared/{sharedLink.LinkToken}";

                // Send notification asynchronously (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await emailService.SendSharedLinkCreatedNotificationAsync(
                            recipientEmail: createdByUser.Email!,
                            recipientName: createdByUser.FullName,
                            documentName: document.DocumentName,
                            linkUrl: linkUrl,
                            expiresAt: expiresAt);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to send shared link created notification to {createdByUser.Email}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail the link creation if email fails
            _logger.LogWarning(ex, "Failed to send shared link created notification");
        }

        return sharedLink;
    }

    /// <summary>
    /// Gets a shared link by token.
    /// </summary>
    public async Task<SharedLink?> GetSharedLinkByTokenAsync(string token)
    {
        return await _context.SharedLinks
            .Include(l => l.Document)
                .ThenInclude(d => d.UploadedByUser)
            .Include(l => l.CreatedByUser)
            .FirstOrDefaultAsync(l => l.LinkToken == token);
    }

    /// <summary>
    /// Validates a shared link (checks expiration, access count, password).
    /// </summary>
    public async Task<ValidationResult> ValidateSharedLinkAsync(string token, string? password = null)
    {
        var result = new ValidationResult();

        var link = await GetSharedLinkByTokenAsync(token);
        if (link == null)
        {
            result.IsValid = false;
            result.ErrorMessage = "الرابط غير موجود أو غير صحيح.";
            return result;
        }

        // Check if link is active
        if (!link.IsActive)
        {
            result.IsValid = false;
            result.ErrorMessage = "الرابط غير مفعّل.";
            return result;
        }

        // Check expiration
        if (link.ExpiresAt < DateTime.UtcNow)
        {
            result.IsValid = false;
            result.ErrorMessage = "الرابط منتهي الصلاحية.";
            return result;
        }

        // Check access count
        if (link.MaxAccessCount.HasValue && link.CurrentAccessCount >= link.MaxAccessCount.Value)
        {
            result.IsValid = false;
            result.ErrorMessage = "تم تجاوز الحد الأقصى لعدد مرات الوصول.";
            return result;
        }

        // Check password if required
        if (!string.IsNullOrEmpty(link.PasswordHash))
        {
            if (string.IsNullOrEmpty(password))
            {
                result.IsValid = false;
                result.ErrorMessage = "هذا الرابط محمي بكلمة مرور.";
                result.Link = link; // Return link so password can be requested
                return result;
            }

            if (!VerifyPassword(password, link.PasswordHash))
            {
                result.IsValid = false;
                result.ErrorMessage = "كلمة المرور غير صحيحة.";
                return result;
            }
        }

        result.IsValid = true;
        result.Link = link;
        return result;
    }

    /// <summary>
    /// Records access to a shared link.
    /// </summary>
    public async Task RecordAccessAsync(int linkId, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            var link = await _context.SharedLinks.FindAsync(linkId);
            if (link == null)
                return;

            // Increment access count
            link.CurrentAccessCount++;

            // Log access
            var accessLog = new LinkAccessLog
            {
                LinkId = linkId,
                AccessedAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            _context.LinkAccessLogs.Add(accessLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Recorded access to shared link {linkId}");

            // Send notification email to the link creator (if email notifications are enabled)
            try
            {
                link = await _context.SharedLinks
                    .Include(l => l.Document)
                    .Include(l => l.CreatedByUser)
                    .FirstOrDefaultAsync(l => l.LinkId == linkId);

                if (link?.CreatedByUser != null && 
                    !string.IsNullOrEmpty(link.CreatedByUser.Email) &&
                    link.Document != null)
                {
                    var emailService = _serviceProvider.GetRequiredService<IEmailService>();
                    // Get base URL from configuration or use default
                    var baseUrl = _configuration["EmailNotifications:BaseUrl"] 
                        ?? "https://yourdomain.com"; // Fallback if not configured
                    var linkUrl = $"{baseUrl.TrimEnd('/')}/shared/{link.LinkToken}";

                    // Send notification asynchronously (fire and forget)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await emailService.SendSharedLinkAccessedNotificationAsync(
                                recipientEmail: link.CreatedByUser.Email!,
                                recipientName: link.CreatedByUser.FullName,
                                documentName: link.Document.DocumentName,
                                linkUrl: linkUrl,
                                accessCount: link.CurrentAccessCount);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to send shared link accessed notification to {link.CreatedByUser.Email}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail the access recording if email fails
                _logger.LogWarning(ex, "Failed to send shared link accessed notification");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error recording access to shared link {linkId}");
        }
    }

    /// <summary>
    /// Gets all shared links for a document.
    /// </summary>
    public async Task<List<SharedLink>> GetSharedLinksByDocumentAsync(int documentId)
    {
        return await _context.SharedLinks
            .Where(l => l.DocumentId == documentId)
            .Include(l => l.CreatedByUser)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all shared links created by a user.
    /// </summary>
    public async Task<List<SharedLink>> GetSharedLinksByUserAsync(int userId)
    {
        return await _context.SharedLinks
            .Where(l => l.CreatedBy == userId)
            .Include(l => l.Document)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Deactivates a shared link.
    /// </summary>
    public async Task DeactivateLinkAsync(int linkId)
    {
        var link = await _context.SharedLinks.FindAsync(linkId);
        if (link != null)
        {
            link.IsActive = false;
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Deactivated shared link {linkId}");
        }
    }

    /// <summary>
    /// Deletes a shared link.
    /// </summary>
    public async Task DeleteLinkAsync(int linkId)
    {
        var link = await _context.SharedLinks
            .Include(l => l.Document)
            .FirstOrDefaultAsync(l => l.LinkId == linkId);

        if (link != null)
        {
            var documentName = link.Document?.DocumentName ?? "Unknown";
            var documentId = link.DocumentId;
            var accessCount = link.CurrentAccessCount;

            _context.SharedLinks.Remove(link);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Deleted shared link {linkId}");

            // Log shared link deletion
            try
            {
                await _auditService.LogDeleteAsync(
                    entityType: "SharedLink",
                    entityId: linkId,
                    additionalData: $"Deleted link for document: {documentName} (ID: {documentId}), Access count: {accessCount}"
                );
            }
            catch (Exception)
            {
                // Don't throw - audit logging should not break link deletion
            }
        }
    }

    /// <summary>
    /// Gets all shared links in the system.
    /// </summary>
    public async Task<List<SharedLink>> GetAllSharedLinksAsync()
    {
        return await _context.SharedLinks
            .Include(l => l.Document)
            .Include(l => l.CreatedByUser)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Generates a unique token for the shared link.
    /// </summary>
    private string GenerateUniqueToken()
    {
        string token;
        bool isUnique;

        do
        {
            // Generate a random token using GUID
            token = Guid.NewGuid().ToString("N").Substring(0, 32);
            
            // Check if token already exists
            isUnique = !_context.SharedLinks.Any(l => l.LinkToken == token);
        }
        while (!isUnique);

        return token;
    }

    /// <summary>
    /// Hashes a password using BCrypt.Net-Next.
    /// </summary>
    /// <param name="password">The plain text password to hash.</param>
    /// <returns>A BCrypt hash of the password.</returns>
    private string HashPassword(string password)
    {
        // Use BCrypt.Net-Next for secure password hashing
        // BCrypt automatically generates a salt and includes it in the hash
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    /// <summary>
    /// Verifies a password against a BCrypt hash.
    /// </summary>
    /// <param name="password">The plain text password to verify.</param>
    /// <param name="hash">The BCrypt hash to verify against.</param>
    /// <returns>True if the password matches the hash, false otherwise.</returns>
    private bool VerifyPassword(string password, string hash)
    {
        try
        {
            // Use BCrypt.Net-Next to verify the password
            // BCrypt automatically extracts the salt from the hash and verifies the password
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception ex)
        {
            // Log error and return false if verification fails
            _logger.LogWarning(ex, "Error verifying password hash. This might indicate an old hash format.");
            return false;
        }
    }
}

