using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace LegalDocSystem.Models;

/// <summary>
/// Represents a managed legal document with metadata and versions.
/// </summary>
[Table("documents")]
public class Document
{
    /// <summary>
    /// Unique identifier for the document.
    /// </summary>
    [Key]
    [Column("document_id")]
    public int DocumentId { get; set; }

    /// <summary>
    /// Folder identifier referencing the logical structure.
    /// </summary>
    [Column("folder_id")]
    public int? FolderId { get; set; }

    /// <summary>
    /// Folder entity containing the document.
    /// </summary>
    public virtual Folder? Folder { get; set; }

    /// <summary>
    /// Original document name visible to users.
    /// </summary>
    [Required]
    [MaxLength(255)]
    [Column("document_name")]
    public string DocumentName { get; set; } = string.Empty;

    /// <summary>
    /// Document classification (contract, memo, etc.).
    /// </summary>
    [MaxLength(50)]
    [Column("document_type")]
    public string? DocumentType { get; set; }

    /// <summary>
    /// Globally unique identifier used for physical storage.
    /// </summary>
    [Column("file_guid")]
    public Guid FileGuid { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Physical path of the stored file.
    /// </summary>
    [Required]
    [Column("file_path")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    [Column("file_size")]
    public long? FileSize { get; set; }

    /// <summary>
    /// MIME type describing the file content.
    /// </summary>
    [MaxLength(100)]
    [Column("mime_type")]
    public string? MimeType { get; set; }

    /// <summary>
    /// Current version number used for tracking.
    /// </summary>
    [Column("current_version")]
    public int CurrentVersion { get; set; } = 1;

    /// <summary>
    /// Flag indicating whether the document is locked.
    /// </summary>
    [Column("is_locked")]
    public bool IsLocked { get; set; }

    /// <summary>
    /// Identifier of the user who locked the document.
    /// </summary>
    [Column("locked_by")]
    public int? LockedBy { get; set; }

    /// <summary>
    /// Reference to the user who locked the document.
    /// </summary>
    public virtual User? LockedByUser { get; set; }

    /// <summary>
    /// Timestamp when the document was locked.
    /// </summary>
    [Column("locked_at")]
    public DateTime? LockedAt { get; set; }

    /// <summary>
    /// Identifier of the uploader.
    /// </summary>
    [Column("uploaded_by")]
    public int? UploadedBy { get; set; }

    /// <summary>
    /// Reference to the user who uploaded the document.
    /// </summary>
    public virtual User? UploadedByUser { get; set; }

    /// <summary>
    /// Timestamp when the document was uploaded.
    /// </summary>
    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tags used for quick filtering.
    /// </summary>
    [Column("tags", TypeName = "text[]")]
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Flexible metadata stored as JSON.
    /// </summary>
    [Column("metadata", TypeName = "jsonb")]
    public JsonDocument? Metadata { get; set; }

    /// <summary>
    /// TSVECTOR column for full-text search.
    /// </summary>
    [Column("search_vector")]
    public string? SearchVector { get; set; }

    /// <summary>
    /// Collection of versions associated with the document.
    /// </summary>
    public virtual ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();

    /// <summary>
    /// OCR queue entries referencing this document.
    /// </summary>
    public virtual ICollection<OcrQueue> OcrQueueItems { get; set; } = new List<OcrQueue>();

    /// <summary>
    /// Tasks linked to the document.
    /// </summary>
    public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();

    /// <summary>
    /// Outgoing correspondences referencing the document.
    /// </summary>
    public virtual ICollection<Outgoing> OutgoingRecords { get; set; } = new List<Outgoing>();

    /// <summary>
    /// Incoming correspondences referencing the document.
    /// </summary>
    public virtual ICollection<Incoming> IncomingRecords { get; set; } = new List<Incoming>();

    /// <summary>
    /// Email logs referencing the document.
    /// </summary>
    public virtual ICollection<EmailLog> EmailLogs { get; set; } = new List<EmailLog>();

    /// <summary>
    /// Shared links exposing the document.
    /// </summary>
    public virtual ICollection<SharedLink> SharedLinks { get; set; } = new List<SharedLink>();
}

