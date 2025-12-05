using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Defando.Models;

/// <summary>
/// Represents an email sent from the system.
/// </summary>
[Table("email_log")]
public class EmailLog
{
    /// <summary>
    /// Unique identifier for the email log entry.
    /// </summary>
    [Key]
    [Column("email_id")]
    public int EmailId { get; set; }

    /// <summary>
    /// Identifier of the related document.
    /// </summary>
    [Column("document_id")]
    public int? DocumentId { get; set; }

    /// <summary>
    /// Document referenced in the email.
    /// </summary>
    public virtual Document? Document { get; set; }

    /// <summary>
    /// Recipient email address.
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("sent_to")]
    public string SentTo { get; set; } = string.Empty;

    /// <summary>
    /// Email subject line.
    /// </summary>
    [MaxLength(500)]
    [Column("subject")]
    public string? Subject { get; set; }

    /// <summary>
    /// Email body content.
    /// </summary>
    [Column("body")]
    public string? Body { get; set; }

    /// <summary>
    /// Identifier of the sender user.
    /// </summary>
    [Column("sent_by")]
    public int? SentBy { get; set; }

    /// <summary>
    /// User who initiated the email.
    /// </summary>
    public virtual User? SentByUser { get; set; }

    /// <summary>
    /// Timestamp when the email was sent.
    /// </summary>
    [Column("sent_at")]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Delivery status (sent, failed, queued).
    /// </summary>
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "sent";

    /// <summary>
    /// Error message captured on failure.
    /// </summary>
    [Column("error_message")]
    public string? ErrorMessage { get; set; }
}

