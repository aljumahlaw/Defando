using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Defando.Models;

/// <summary>
/// Represents a customizable application configuration entry.
/// </summary>
[Table("settings")]
public class Settings
{
    /// <summary>
    /// Unique key identifying the setting.
    /// </summary>
    [Key]
    [MaxLength(100)]
    [Column("setting_key")]
    public string SettingKey { get; set; } = string.Empty;

    /// <summary>
    /// Stored value (could contain JSON or simple text).
    /// </summary>
    [Column("setting_value")]
    public string? SettingValue { get; set; }

    /// <summary>
    /// Description explaining the usage of the setting.
    /// </summary>
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Timestamp when the setting was last updated.
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

