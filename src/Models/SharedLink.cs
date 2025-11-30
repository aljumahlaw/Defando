using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalDocSystem.Models;

/// <summary>
/// Represents a time-bound link used to share documents externally.
/// </summary>
[Table("shared_links")]
public class SharedLink
{
    /// <summary>
    /// Unique identifier for the shared link.
    /// </summary>
    [Key]
    [Column("link_id")]
    public int LinkId { get; set; }

    /// <summary>
    /// Identifier of the shared document.
    /// </summary>
    [Required]
    [Column("document_id")]
    public int DocumentId { get; set; }

    /// <summary>
    /// Document referenced by the shared link.
    /// </summary>
    public virtual Document Document { get; set; } = null!;

    /// <summary>
    /// Random token composing the public URL.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("link_token")]
    public string LinkToken { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the creator user.
    /// </summary>
    [Column("created_by")]
    public int? CreatedBy { get; set; }

    /// <summary>
    /// User who created the shared link.
    /// </summary>
    public virtual User? CreatedByUser { get; set; }

    /// <summary>
    /// Expiration timestamp of the link.
    /// </summary>
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Maximum allowed number of accesses.
    /// </summary>
    [Column("max_access_count")]
    public int? MaxAccessCount { get; set; }

    /// <summary>
    /// Number of accesses recorded so far.
    /// </summary>
    [Column("current_access_count")]
    public int CurrentAccessCount { get; set; }

    /// <summary>
    /// Optional password hash protecting the link.
    /// </summary>
    [MaxLength(255)]
    [Column("password_hash")]
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Indicates if the link is active.
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp when the link was created.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// List of access logs recorded for this link.
    /// </summary>
    public virtual ICollection<LinkAccessLog> AccessLogs { get; set; } = new List<LinkAccessLog>();
}

