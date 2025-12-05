namespace Defando.Services;

/// <summary>
/// Service interface for background jobs using Hangfire.
/// </summary>
public interface IBackgroundJobsService
{
    /// <summary>
    /// Processes OCR queue items - runs every minute.
    /// </summary>
    Task ProcessOcrQueueAsync();

    /// <summary>
    /// Sends email notifications for pending tasks - runs every hour.
    /// </summary>
    Task SendEmailNotificationsAsync();

    /// <summary>
    /// Cleans up expired shared links - runs daily.
    /// </summary>
    Task CleanupExpiredLinksAsync();

    /// <summary>
    /// Generates audit reports - runs daily at midnight.
    /// </summary>
    Task GenerateAuditReportsAsync();

    /// <summary>
    /// Unlocks documents that have been locked for more than 8 hours - runs every hour.
    /// </summary>
    Task UnlockExpiredDocumentsAsync();
}

