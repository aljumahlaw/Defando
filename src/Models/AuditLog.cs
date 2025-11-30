using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalDocSystem.Models;

/// <summary>
/// Represents an auditable action performed by a user.
/// </summary>
[Table("audit_log")]
public class AuditLog
{
    /// <summary>
    /// Unique identifier for the audit entry.
    /// </summary>
    [Key]
    [Column("log_id")]
    public int LogId { get; set; }

    /// <summary>
    /// Identifier of the acting user.
    /// </summary>
    [Column("user_id")]
    public int? UserId { get; set; }

    /// <summary>
    /// Associated user entity.
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// Action keyword (upload, delete, etc.).
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Type of entity affected by the action.
    /// </summary>
    [MaxLength(30)]
    [Column("entity_type")]
    public string? EntityType { get; set; }

    /// <summary>
    /// Identifier of the entity affected by the action.
    /// </summary>
    [Column("entity_id")]
    public int? EntityId { get; set; }

    /// <summary>
    /// Additional context stored as text.
    /// </summary>
    [Column("details")]
    public string? Details { get; set; }

    /// <summary>
    /// IP address of the client when the action occurred.
    /// </summary>
    [MaxLength(45)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Timestamp of when the action occurred.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

