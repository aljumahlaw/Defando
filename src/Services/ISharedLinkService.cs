using Defando.Models;

namespace Defando.Services;

/// <summary>
/// Service interface for managing shared links.
/// </summary>
public interface ISharedLinkService
{
    /// <summary>
    /// Creates a new shared link for a document.
    /// </summary>
    Task<SharedLink> CreateSharedLinkAsync(
        int documentId,
        int createdBy,
        DateTime expiresAt,
        int? maxAccessCount = null,
        string? password = null);

    /// <summary>
    /// Gets a shared link by token.
    /// </summary>
    Task<SharedLink?> GetSharedLinkByTokenAsync(string token);

    /// <summary>
    /// Validates a shared link (checks expiration, access count, password).
    /// </summary>
    Task<ValidationResult> ValidateSharedLinkAsync(string token, string? password = null);

    /// <summary>
    /// Records access to a shared link.
    /// </summary>
    Task RecordAccessAsync(int linkId, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Gets all shared links for a document.
    /// </summary>
    Task<List<SharedLink>> GetSharedLinksByDocumentAsync(int documentId);

    /// <summary>
    /// Gets all shared links created by a user.
    /// </summary>
    Task<List<SharedLink>> GetSharedLinksByUserAsync(int userId);

    /// <summary>
    /// Deactivates a shared link.
    /// </summary>
    Task DeactivateLinkAsync(int linkId);

    /// <summary>
    /// Deletes a shared link.
    /// </summary>
    Task DeleteLinkAsync(int linkId);

    /// <summary>
    /// Gets all shared links in the system.
    /// </summary>
    Task<List<SharedLink>> GetAllSharedLinksAsync();
}

/// <summary>
/// Result of shared link validation.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public SharedLink? Link { get; set; }
}

