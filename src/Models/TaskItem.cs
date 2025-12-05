using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Defando.Models;

/// <summary>
/// Represents a task assigned within the legal team.
/// </summary>
[Table("tasks")]
public class TaskItem
{
    /// <summary>
    /// Unique identifier for the task.
    /// </summary>
    [Key]
    [Column("task_id")]
    public int TaskId { get; set; }

    /// <summary>
    /// Title describing the task goal.
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("task_title")]
    public string TaskTitle { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the task.
    /// </summary>
    [Column("task_description")]
    public string? TaskDescription { get; set; }

    /// <summary>
    /// User identifier for the assignee.
    /// </summary>
    [Required]
    [Column("assigned_to")]
    public int AssignedTo { get; set; }

    /// <summary>
    /// User who is responsible for completing the task.
    /// </summary>
    public virtual User AssignedToUser { get; set; } = null!;

    /// <summary>
    /// User identifier for the creator.
    /// </summary>
    [Required]
    [Column("assigned_by")]
    public int AssignedBy { get; set; }

    /// <summary>
    /// User who created and assigned the task.
    /// </summary>
    public virtual User AssignedByUser { get; set; } = null!;

    /// <summary>
    /// Optional linked document identifier.
    /// </summary>
    [Column("document_id")]
    public int? DocumentId { get; set; }

    /// <summary>
    /// Document associated with the task.
    /// </summary>
    public virtual Document? Document { get; set; }

    /// <summary>
    /// Priority level (low, normal, high, critical).
    /// </summary>
    [MaxLength(20)]
    [Column("priority")]
    public string Priority { get; set; } = "normal";

    /// <summary>
    /// Current status of the task.
    /// </summary>
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Due date for completing the task.
    /// </summary>
    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Timestamp when the task was completed.
    /// </summary>
    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Additional notes associated with the task.
    /// </summary>
    [Column("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last updated timestamp.
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Comments left on the task.
    /// </summary>
    public virtual ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
}

