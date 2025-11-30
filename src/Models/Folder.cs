using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalDocSystem.Models;

/// <summary>
/// Describes a logical folder used to organize documents.
/// </summary>
[Table("folders")]
public class Folder
{
    /// <summary>
    /// Unique identifier for the folder.
    /// </summary>
    [Key]
    [Column("folder_id")]
    public int FolderId { get; set; }

    /// <summary>
    /// Identifier of the parent folder if exists.
    /// </summary>
    [Column("parent_id")]
    public int? ParentId { get; set; }

    /// <summary>
    /// Reference to the parent folder.
    /// </summary>
    public virtual Folder? ParentFolder { get; set; }

    /// <summary>
    /// Display name of the folder.
    /// </summary>
    [Required]
    [MaxLength(150)]
    [Column("folder_name")]
    public string FolderName { get; set; } = string.Empty;

    /// <summary>
    /// Logical path shown to users.
    /// </summary>
    [Required]
    [Column("folder_path")]
    public string FolderPath { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the user who created the folder.
    /// </summary>
    [Column("created_by")]
    public int? CreatedBy { get; set; }

    /// <summary>
    /// User who created the folder.
    /// </summary>
    public virtual User? CreatedByUser { get; set; }

    /// <summary>
    /// Timestamp when the folder was created.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Child folders contained inside this folder.
    /// </summary>
    public virtual ICollection<Folder> ChildFolders { get; set; } = new List<Folder>();

    /// <summary>
    /// Documents stored inside this folder.
    /// </summary>
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}

