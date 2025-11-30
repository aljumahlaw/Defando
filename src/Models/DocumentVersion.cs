using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalDocSystem.Models;

/// <summary>
/// Represents a single version of a document.
/// </summary>
[Table("document_versions")]
public class DocumentVersion
{
    /// <summary>
    /// Unique identifier for the version.
    /// </summary>
    [Key]
    [Column("version_id")]
    public int VersionId { get; set; }

    /// <summary>
    /// Identifier of the owning document.
    /// </summary>
    [Required]
    [Column("document_id")]
    public int DocumentId { get; set; }

    /// <summary>
    /// Parent document entity.
    /// </summary>
    public virtual Document Document { get; set; } = null!;

    /// <summary>
    /// Sequential version number.
    /// </summary>
    [Column("version_number")]
    public int VersionNumber { get; set; }

    /// <summary>
    /// Physical path to the stored file for this version.
    /// </summary>
    [Required]
    [Column("file_path")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Size of the stored file in bytes.
    /// </summary>
    [Column("file_size")]
    public long? FileSize { get; set; }

    /// <summary>
    /// Indicates whether the version is marked as final.
    /// </summary>
    [Column("is_final")]
    public bool IsFinal { get; set; }

    /// <summary>
    /// Description of the changes introduced by this version.
    /// </summary>
    [Column("change_description")]
    public string? ChangeDescription { get; set; }

    /// <summary>
    /// Identifier of the user who created the version.
    /// </summary>
    [Column("created_by")]
    public int? CreatedBy { get; set; }

    /// <summary>
    /// Reference to the user who created the version.
    /// </summary>
    public virtual User? CreatedByUser { get; set; }

    /// <summary>
    /// Timestamp when the version was created.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

