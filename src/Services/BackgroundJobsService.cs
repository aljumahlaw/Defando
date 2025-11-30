using LegalDocSystem.Data;
using LegalDocSystem.Helpers;
using LegalDocSystem.Models;
using LegalDocSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace LegalDocSystem.Services;

/// <summary>
/// Service for background jobs using Hangfire.
/// </summary>
public class BackgroundJobsService : IBackgroundJobsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BackgroundJobsService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BackgroundJobsService(
        ApplicationDbContext context, 
        ILogger<BackgroundJobsService> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Processes OCR queue items - runs every minute.
    /// </summary>
    public async Task ProcessOcrQueueAsync()
    {
        try
        {
            _logger.LogInformation("Starting OCR queue processing...");

            var pendingItems = await _context.OcrQueue
                .Where(q => q.Status == "pending")
                .OrderBy(q => q.CreatedAt)
                .Take(10) // Process 10 items at a time
                .ToListAsync();

            foreach (var item in pendingItems)
            {
                try
                {
                    // TODO: Implement OCR processing using Tesseract
                    // 1. Load file from NAS storage using file_guid
                    // 2. Run Tesseract OCR with Arabic language
                    // 3. Extract text and save to document.metadata->>'ocr_text'
                    // 4. Update document.search_vector
                    // 5. Update OCR queue item status to 'completed'

                    item.Status = "processing";
                    item.ProcessedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // Simulate OCR processing
                    await Task.Delay(1000);

                    item.Status = "completed";
                    item.CompletedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"OCR processing completed for queue item {item.QueueId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing OCR queue item {item.QueueId}");
                    item.Status = "failed";
                    item.ErrorMessage = ex.Message;
                    await _context.SaveChangesAsync();
                }
            }

            _logger.LogInformation($"OCR queue processing completed. Processed {pendingItems.Count} items.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessOcrQueueAsync");
        }
    }

    /// <summary>
    /// Sends email notifications for pending tasks - runs every hour.
    /// </summary>
    public async Task SendEmailNotificationsAsync()
    {
        try
        {
            _logger.LogInformation("Starting email notifications processing...");

            // Get tasks due in 24 hours
            var dueSoonTasks = await _context.Tasks
                .Where(t => t.Status != "completed" && 
                           t.Status != "cancelled" &&
                           t.DueDate.HasValue &&
                           t.DueDate.Value <= DateTime.UtcNow.AddHours(24) &&
                           t.DueDate.Value > DateTime.UtcNow)
                .Include(t => t.AssignedToUser)
                .ToListAsync();

            foreach (var task in dueSoonTasks)
            {
                try
                {
                    // Send email notification using EmailService
                    if (task.AssignedToUser != null && !string.IsNullOrEmpty(task.AssignedToUser.Email))
                    {
                        var emailService = _serviceProvider.GetRequiredService<IEmailService>();
                        
                        // Use email template for task reminder
                        var body = EmailTemplates.TaskReminder(
                            recipientName: task.AssignedToUser.FullName,
                            taskTitle: task.Title,
                            dueDate: task.DueDate.Value,
                            taskStatus: task.Status);

                        var subject = $"تذكير: مهمة قريبة من الاستحقاق - {task.Title}";

                        // Use SendEmailWithRetryAsync for better reliability
                        await emailService.SendEmailWithRetryAsync(
                            to: task.AssignedToUser.Email,
                            subject: subject,
                            body: body,
                            isHtml: true,
                            maxRetries: 3
                        );

                        _logger.LogInformation($"Reminder email sent for task {task.TaskId} to {task.AssignedToUser.Email}");
                    }
                    else
                    {
                        _logger.LogWarning($"Task {task.TaskId} has no assigned user or email address");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error sending email notification for task {task.TaskId}");
                }
            }

            _logger.LogInformation($"Email notifications processing completed. Processed {dueSoonTasks.Count} tasks.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendEmailNotificationsAsync");
        }
    }

    /// <summary>
    /// Cleans up expired shared links - runs daily.
    /// </summary>
    public async Task CleanupExpiredLinksAsync()
    {
        try
        {
            _logger.LogInformation("Starting expired links cleanup...");

            var expiredLinks = await _context.SharedLinks
                .Where(l => l.ExpiresAt.HasValue && l.ExpiresAt.Value < DateTime.UtcNow)
                .ToListAsync();

            foreach (var link in expiredLinks)
            {
                link.IsActive = false;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Expired links cleanup completed. Deactivated {expiredLinks.Count} links.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CleanupExpiredLinksAsync");
        }
    }

    /// <summary>
    /// Generates audit reports - runs daily at midnight.
    /// </summary>
    public async Task GenerateAuditReportsAsync()
    {
        try
        {
            _logger.LogInformation("Starting audit report generation...");

            var yesterday = DateTime.UtcNow.AddDays(-1).Date;
            var today = DateTime.UtcNow.Date;

            // Get audit logs from yesterday
            var auditLogs = await _context.AuditLogs
                .Where(a => a.CreatedAt >= yesterday && a.CreatedAt < today)
                .ToListAsync();

            // TODO: Generate report
            // 1. Group by action type
            // 2. Count actions per user
            // 3. Generate PDF report using iText7
            // 4. Save report to NAS storage
            // 5. Send report to admin via email

            _logger.LogInformation($"Audit report generation completed. Processed {auditLogs.Count} log entries.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GenerateAuditReportsAsync");
        }
    }

    /// <summary>
    /// Unlocks documents that have been locked for more than 8 hours - runs every hour.
    /// </summary>
    public async Task UnlockExpiredDocumentsAsync()
    {
        try
        {
            _logger.LogInformation("Starting expired document locks cleanup...");

            var eightHoursAgo = DateTime.UtcNow.AddHours(-8);

            var lockedDocuments = await _context.Documents
                .Where(d => d.IsLocked && 
                           d.LockedAt.HasValue && 
                           d.LockedAt.Value < eightHoursAgo)
                .ToListAsync();

            foreach (var document in lockedDocuments)
            {
                document.IsLocked = false;
                document.LockedAt = null;
                document.LockedBy = null;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Expired document locks cleanup completed. Unlocked {lockedDocuments.Count} documents.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UnlockExpiredDocumentsAsync");
        }
    }
}

