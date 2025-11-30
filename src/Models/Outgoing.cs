using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalDocSystem.Models;

/// <summary>
/// Represents an outgoing correspondence record.
/// </summary>
[Table("outgoing")]
public class Outgoing
{
    /// <summary>
    /// Unique identifier for the outgoing record.
    /// </summary>
    [Key]
    [Column("outgoing_id")]
    public int OutgoingId { get; set; }

    /// <summary>
    /// Identifier of the linked document.
    /// </summary>
    [Column("document_id")]
    public int? DocumentId { get; set; }

    /// <summary>
    /// Associated document entity.
    /// </summary>
    public virtual Document? Document { get; set; }

    /// <summary>
    /// Sequential outgoing number (ORG-OUT-YYYY-XXXX).
    /// </summary>
    [Required]
    [MaxLength(60)]
    [Column("outgoing_number")]
    public string OutgoingNumber { get; set; } = string.Empty;

    /// <summary>
    /// Name of the recipient.
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("recipient_name")]
    public string RecipientName { get; set; } = string.Empty;

    /// <summary>
    /// Address of the recipient.
    /// </summary>
    [Column("recipient_address")]
    public string? RecipientAddress { get; set; }

    /// <summary>
    /// Subject line of the correspondence.
    /// </summary>
    [Required]
    [MaxLength(500)]
    [Column("subject")]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Date when the mail was sent.
    /// </summary>
    [Column("send_date")]
    public DateTime SendDate { get; set; } = DateTime.UtcNow.Date;

    /// <summary>
    /// Delivery channel (Manual, Email, etc.).
    /// </summary>
    [MaxLength(50)]
    [Column("delivery_method")]
    public string? DeliveryMethod { get; set; }

    /// <summary>
    /// Optional tracking number.
    /// </summary>
    [MaxLength(120)]
    [Column("tracking_number")]
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Additional notes.
    /// </summary>
    [Column("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// Identifier of the user who created the record.
    /// </summary>
    [Column("created_by")]
    public int? CreatedBy { get; set; }

    /// <summary>
    /// User entity who created the record.
    /// </summary>
    public virtual User? CreatedByUser { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

