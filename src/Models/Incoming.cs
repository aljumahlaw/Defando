using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalDocSystem.Models;

/// <summary>
/// Represents an incoming correspondence record.
/// </summary>
[Table("incoming")]
public class Incoming
{
    /// <summary>
    /// Unique identifier for the incoming record.
    /// </summary>
    [Key]
    [Column("incoming_id")]
    public int IncomingId { get; set; }

    /// <summary>
    /// Identifier of the linked document.
    /// </summary>
    [Column("document_id")]
    public int? DocumentId { get; set; }

    /// <summary>
    /// Document entity associated with the incoming record.
    /// </summary>
    public virtual Document? Document { get; set; }

    /// <summary>
    /// Sequential incoming number (ORG-IN-YYYY-XXXX).
    /// </summary>
    [Required]
    [MaxLength(60)]
    [Column("incoming_number")]
    public string IncomingNumber { get; set; } = string.Empty;

    /// <summary>
    /// Name of the sender.
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("sender_name")]
    public string SenderName { get; set; } = string.Empty;

    /// <summary>
    /// Address of the sender.
    /// </summary>
    [Column("sender_address")]
    public string? SenderAddress { get; set; }

    /// <summary>
    /// Subject line provided by the sender.
    /// </summary>
    [Required]
    [MaxLength(500)]
    [Column("subject")]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Date when the correspondence was received.
    /// </summary>
    [Column("received_date")]
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow.Date;

    /// <summary>
    /// Original sender number if provided.
    /// </summary>
    [MaxLength(120)]
    [Column("original_number")]
    public string? OriginalNumber { get; set; }

    /// <summary>
    /// Priority flag (normal, urgent, confidential).
    /// </summary>
    [MaxLength(20)]
    [Column("priority")]
    public string Priority { get; set; } = "normal";

    /// <summary>
    /// Indicates if a response is required.
    /// </summary>
    [Column("requires_response")]
    public bool RequiresResponse { get; set; }

    /// <summary>
    /// Deadline for the required response.
    /// </summary>
    [Column("response_deadline")]
    public DateTime? ResponseDeadline { get; set; }

    /// <summary>
    /// Additional notes captured during registration.
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

