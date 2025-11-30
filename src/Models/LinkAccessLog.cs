using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalDocSystem.Models;

/// <summary>
/// Represents an access event for a shared link.
/// </summary>
[Table("link_access_log")]
public class LinkAccessLog
{
    /// <summary>
    /// Unique identifier for the access entry.
    /// </summary>
    [Key]
    [Column("access_id")]
    public int AccessId { get; set; }

    /// <summary>
    /// Identifier of the shared link accessed.
    /// </summary>
    [Required]
    [Column("link_id")]
    public int LinkId { get; set; }

    /// <summary>
    /// Shared link associated with this access.
    /// </summary>
    public virtual SharedLink SharedLink { get; set; } = null!;

    /// <summary>
    /// Timestamp when the link was accessed.
    /// </summary>
    [Column("accessed_at")]
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address of the client.
    /// </summary>
    [MaxLength(45)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string of the client.
    /// </summary>
    [Column("user_agent")]
    public string? UserAgent { get; set; }
}

