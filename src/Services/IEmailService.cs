using Defando.Models;

namespace Defando.Services;

/// <summary>
/// Service interface for sending emails via SMTP.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email to the specified recipient.
    /// </summary>
    Task<bool> SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        List<string>? attachments = null,
        string? cc = null,
        string? bcc = null);

    /// <summary>
    /// Sends a test email to verify SMTP configuration.
    /// </summary>
    Task<bool> SendTestEmailAsync(string toEmail);

    /// <summary>
    /// Validates SMTP configuration settings.
    /// </summary>
    Task<bool> ValidateSmtpSettingsAsync();

    /// <summary>
    /// Gets current SMTP configuration.
    /// </summary>
    Task<SmtpSettings?> GetSmtpSettingsAsync();

    /// <summary>
    /// Saves SMTP configuration to database.
    /// </summary>
    Task SaveSmtpSettingsAsync(SmtpSettings settings);

    /// <summary>
    /// Sends an email with retry logic.
    /// </summary>
    Task<bool> SendEmailWithRetryAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        int maxRetries = 3,
        int delaySeconds = 5);

    /// <summary>
    /// Sends a notification email when a shared link is created.
    /// </summary>
    Task<bool> SendSharedLinkCreatedNotificationAsync(
        string recipientEmail,
        string recipientName,
        string documentName,
        string linkUrl,
        DateTime expiresAt);

    /// <summary>
    /// Sends a notification email when a shared link is accessed.
    /// </summary>
    Task<bool> SendSharedLinkAccessedNotificationAsync(
        string recipientEmail,
        string recipientName,
        string documentName,
        string linkUrl,
        int accessCount);
}

