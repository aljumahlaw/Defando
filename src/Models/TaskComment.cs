using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Defando.Models;

/// <summary>
/// Represents a comment added to a task.
/// </summary>
[Table("task_comments")]
public class TaskComment
{
    /// <summary>
    /// Unique identifier for the comment.
    /// </summary>
    [Key]
    [Column("comment_id")]
    public int CommentId { get; set; }

    /// <summary>
    /// Identifier of the associated task.
    /// </summary>
    [Required]
    [Column("task_id")]
    public int TaskId { get; set; }

    /// <summary>
    /// Task entity referenced by the comment.
    /// </summary>
    public virtual TaskItem Task { get; set; } = null!;

    /// <summary>
    /// Identifier of the authoring user.
    /// </summary>
    [Column("user_id")]
    public int? UserId { get; set; }

    /// <summary>
    /// User who created the comment.
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// Body of the comment.
    /// </summary>
    [Required]
    [Column("comment_text")]
    public string CommentText { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the comment was created.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

