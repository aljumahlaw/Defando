using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalDocSystem.Models;

/// <summary>
/// Represents an OCR processing queue entry.
/// </summary>
[Table("ocr_queue")]
public class OcrQueue
{
    /// <summary>
    /// Unique identifier for the queue entry.
    /// </summary>
    [Key]
    [Column("queue_id")]
    public int QueueId { get; set; }

    /// <summary>
    /// Identifier of the document awaiting OCR.
    /// </summary>
    [Required]
    [Column("document_id")]
    public int DocumentId { get; set; }

    /// <summary>
    /// Reference to the document entity.
    /// </summary>
    public virtual Document Document { get; set; } = null!;

    /// <summary>
    /// Processing status (pending, processing, completed, failed).
    /// </summary>
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Error details when the OCR job fails.
    /// </summary>
    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when the entry was created.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when processing was completed.
    /// </summary>
    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }
}

