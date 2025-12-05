using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Defando.Models;

/// <summary>
/// Represents an audit log entry with detailed information about an action.
/// This is a DTO/ViewModel used by AuditService for creating audit logs.
/// </summary>
public class AuditLogEntry
{
    /// <summary>
    /// The type of event (e.g., "Login", "Logout", "Create", "Update", "Delete").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Event { get; set; } = string.Empty;

    /// <summary>
    /// Category of the event (e.g., "Authentication", "Document", "User", "Folder").
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; set; }

    /// <summary>
    /// The action performed (e.g., "login", "create_document", "delete_folder").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the subject (user) who performed the action.
    /// </summary>
    public int? SubjectIdentifier { get; set; }

    /// <summary>
    /// Name of the subject (username or full name).
    /// </summary>
    [MaxLength(150)]
    public string? SubjectName { get; set; }

    /// <summary>
    /// Type of the subject (e.g., "User", "System").
    /// </summary>
    [MaxLength(30)]
    public string? SubjectType { get; set; }

    /// <summary>
    /// Type of entity affected by the action (e.g., "Document", "Folder", "User").
    /// </summary>
    [MaxLength(30)]
    public string? EntityType { get; set; }

    /// <summary>
    /// Identifier of the entity affected by the action.
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>
    /// Additional data stored as JSON or text.
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// IP address of the client when the action occurred.
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the client (browser/client information).
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Timestamp of when the action occurred.
    /// </summary>
    public DateTime Created { get; set; } = DateTime.UtcNow;
}

