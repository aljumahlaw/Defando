using LegalDocSystem.Data;
using LegalDocSystem.Helpers;
using LegalDocSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace LegalDocSystem.Services;

/// <summary>
/// Service implementation for sending emails via SMTP using MailKit.
/// </summary>
public class EmailService : IEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailService> _logger;
    private readonly IEncryptionService _encryptionService;
    private SmtpSettings? _cachedSettings;

    public EmailService(
        ApplicationDbContext context, 
        ILogger<EmailService> logger,
        IEncryptionService encryptionService)
    {
        _context = context;
        _logger = logger;
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// Sends an email to the specified recipient.
    /// </summary>
    public async Task<bool> SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        List<string>? attachments = null,
        string? cc = null,
        string? bcc = null)
    {
        try
        {
            var settings = await GetSmtpSettingsAsync();
            if (settings == null || !settings.Enabled)
            {
                _logger.LogWarning("SMTP is not configured or disabled. Email not sent.");
                return false;
            }

            _logger.LogInformation($"Sending email to {to} with subject: {subject}");

            // Log email attempt
            var emailLog = new EmailLog
            {
                SentTo = to,
                Subject = subject,
                Body = body,
                SentAt = DateTime.UtcNow,
                Status = "pending"
            };

            _context.EmailLogs.Add(emailLog);

            try
            {
                // Create MIME message
                using var message = new MimeMessage();
                message.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
                message.To.Add(MailboxAddress.Parse(to));
                
                if (!string.IsNullOrEmpty(cc))
                    message.Cc.Add(MailboxAddress.Parse(cc));
                
                if (!string.IsNullOrEmpty(bcc))
                    message.Bcc.Add(MailboxAddress.Parse(bcc));
                
                message.Subject = subject;

                // Build message body
                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                    bodyBuilder.HtmlBody = body;
                else
                    bodyBuilder.TextBody = body;

                // Add attachments if provided
                if (attachments != null && attachments.Any())
                {
                    foreach (var attachmentPath in attachments)
                    {
                        if (File.Exists(attachmentPath))
                        {
                            bodyBuilder.Attachments.Add(attachmentPath);
                            _logger.LogDebug($"Added attachment: {attachmentPath}");
                        }
                        else
                        {
                            _logger.LogWarning($"Attachment file not found: {attachmentPath}");
                        }
                    }
                }

                message.Body = bodyBuilder.ToMessageBody();

                // Connect to SMTP server and send email
                using var client = new SmtpClient();
                
                // Determine secure socket options
                var secureSocketOptions = settings.UseSsl 
                    ? SecureSocketOptions.StartTls 
                    : SecureSocketOptions.None;

                _logger.LogDebug($"Connecting to SMTP server: {settings.Host}:{settings.Port} (SSL: {settings.UseSsl})");
                
                await client.ConnectAsync(settings.Host, settings.Port, secureSocketOptions);
                
                // Authenticate with decrypted password
                string decryptedPassword = _encryptionService.Decrypt(settings.Password);
                await client.AuthenticateAsync(settings.Username, decryptedPassword);
                
                _logger.LogDebug("SMTP authentication successful");
                
                // Send email
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                emailLog.Status = "sent";
                emailLog.SentAt = DateTime.UtcNow;
                _logger.LogInformation($"Email sent successfully to {to}");

                await _context.SaveChangesAsync();
                return true;
            }
            catch (SmtpCommandException ex)
            {
                emailLog.Status = "failed";
                emailLog.ErrorMessage = $"SMTP Command Error: {ex.Message} (Status: {ex.StatusCode})";
                _logger.LogError(ex, $"SMTP command error while sending email to {to}. Status: {ex.StatusCode}");
                await _context.SaveChangesAsync();
                return false;
            }
            catch (SmtpProtocolException ex)
            {
                emailLog.Status = "failed";
                emailLog.ErrorMessage = $"SMTP Protocol Error: {ex.Message}";
                _logger.LogError(ex, $"SMTP protocol error while sending email to {to}");
                await _context.SaveChangesAsync();
                return false;
            }
            catch (Exception ex)
            {
                emailLog.Status = "failed";
                emailLog.ErrorMessage = ex.Message;
                _logger.LogError(ex, $"Failed to send email to {to}");
                await _context.SaveChangesAsync();
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendEmailAsync");
            return false;
        }
    }

    /// <summary>
    /// Sends a test email to verify SMTP configuration.
    /// </summary>
    public async Task<bool> SendTestEmailAsync(string toEmail)
    {
        try
        {
            var settings = await GetSmtpSettingsAsync();
            if (settings == null || !settings.Enabled)
            {
                _logger.LogWarning("SMTP is not configured or disabled.");
                return false;
            }

            var subject = "اختبار إعدادات SMTP - نظام إدارة المستندات القانونية";
            var body = $@"
                <html dir=""rtl"">
                <body>
                    <h2>اختبار إعدادات البريد الإلكتروني</h2>
                    <p>هذه رسالة اختبار للتحقق من إعدادات SMTP.</p>
                    <p>إذا تلقيت هذه الرسالة، فهذا يعني أن الإعدادات صحيحة.</p>
                    <hr>
                    <p><small>تم الإرسال في: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</small></p>
                </body>
                </html>";

            return await SendEmailAsync(toEmail, subject, body, isHtml: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test email");
            return false;
        }
    }

    /// <summary>
    /// Validates SMTP configuration settings.
    /// </summary>
    public async Task<bool> ValidateSmtpSettingsAsync()
    {
        try
        {
            var settings = await GetSmtpSettingsAsync();
            if (settings == null)
                return false;

            // Validate required fields
            if (string.IsNullOrWhiteSpace(settings.Host))
                return false;

            if (settings.Port <= 0 || settings.Port > 65535)
                return false;

            if (string.IsNullOrWhiteSpace(settings.Username))
                return false;

            if (string.IsNullOrWhiteSpace(settings.Password))
                return false;

            if (string.IsNullOrWhiteSpace(settings.FromAddress))
                return false;

            // Validate email format
            if (!IsValidEmail(settings.FromAddress))
                return false;

            // Try to connect to SMTP server (without sending)
            try
            {
                using var client = new SmtpClient();
                
                var secureSocketOptions = settings.UseSsl 
                    ? SecureSocketOptions.StartTls 
                    : SecureSocketOptions.None;
                
                await client.ConnectAsync(settings.Host, settings.Port, secureSocketOptions);
                
                // Authenticate with decrypted password
                string decryptedPassword = _encryptionService.Decrypt(settings.Password);
                await client.AuthenticateAsync(settings.Username, decryptedPassword);
                
                await client.DisconnectAsync(true);
                
                _logger.LogInformation("SMTP settings validated successfully");
                return true;
            }
            catch (SmtpCommandException ex)
            {
                _logger.LogWarning(ex, $"SMTP validation failed: {ex.Message} (Status: {ex.StatusCode})");
                return false;
            }
            catch (SmtpProtocolException ex)
            {
                _logger.LogWarning(ex, $"SMTP protocol error during validation: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"SMTP validation failed: {ex.Message}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating SMTP settings");
            return false;
        }
    }

    /// <summary>
    /// Gets current SMTP configuration from database.
    /// </summary>
    public async Task<SmtpSettings?> GetSmtpSettingsAsync()
    {
        try
        {
            // Return cached settings if available
            if (_cachedSettings != null)
                return _cachedSettings;

            var settings = await _context.Settings.ToListAsync();
            var smtpSettings = new SmtpSettings();

            foreach (var setting in settings)
            {
                switch (setting.SettingKey)
                {
                    case "smtp_host":
                        smtpSettings.Host = setting.SettingValue ?? string.Empty;
                        break;
                    case "smtp_port":
                        if (int.TryParse(setting.SettingValue, out var port))
                            smtpSettings.Port = port;
                        break;
                    case "smtp_username":
                        smtpSettings.Username = setting.SettingValue ?? string.Empty;
                        break;
                    case "smtp_password":
                        smtpSettings.Password = setting.SettingValue ?? string.Empty;
                        break;
                    case "smtp_use_ssl":
                        if (bool.TryParse(setting.SettingValue, out var useSsl))
                            smtpSettings.UseSsl = useSsl;
                        break;
                    case "email_from_name":
                        smtpSettings.FromName = setting.SettingValue ?? string.Empty;
                        break;
                    case "email_from_address":
                        smtpSettings.FromAddress = setting.SettingValue ?? string.Empty;
                        break;
                    case "smtp_enabled":
                        if (bool.TryParse(setting.SettingValue, out var enabled))
                            smtpSettings.Enabled = enabled;
                        break;
                    case "notify_shared_link_created":
                        if (bool.TryParse(setting.SettingValue, out var notifyCreated))
                            smtpSettings.NotifyOnSharedLinkCreated = notifyCreated;
                        break;
                    case "notify_shared_link_accessed":
                        if (bool.TryParse(setting.SettingValue, out var notifyAccessed))
                            smtpSettings.NotifyOnSharedLinkAccessed = notifyAccessed;
                        break;
                    case "notify_task_reminder":
                        if (bool.TryParse(setting.SettingValue, out var notifyTask))
                            smtpSettings.NotifyOnTaskReminder = notifyTask;
                        break;
                }
            }

            // Cache settings
            _cachedSettings = smtpSettings;
            return smtpSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SMTP settings");
            return null;
        }
    }

    /// <summary>
    /// Saves SMTP configuration to database.
    /// </summary>
    public async Task SaveSmtpSettingsAsync(SmtpSettings settings)
    {
        try
        {
            var settingsToSave = new Dictionary<string, string>
            {
                { "smtp_host", settings.Host },
                { "smtp_port", settings.Port.ToString() },
                { "smtp_username", settings.Username },
                { "smtp_password", _encryptionService.Encrypt(settings.Password) }, // Encrypt password using secure encryption
                { "smtp_use_ssl", settings.UseSsl.ToString() },
                { "email_from_name", settings.FromName },
                { "email_from_address", settings.FromAddress },
                { "smtp_enabled", settings.Enabled.ToString() },
                { "notify_shared_link_created", settings.NotifyOnSharedLinkCreated.ToString() },
                { "notify_shared_link_accessed", settings.NotifyOnSharedLinkAccessed.ToString() },
                { "notify_task_reminder", settings.NotifyOnTaskReminder.ToString() }
            };

            foreach (var kvp in settingsToSave)
            {
                var existing = await _context.Settings.FindAsync(kvp.Key);
                if (existing != null)
                {
                    existing.SettingValue = kvp.Value;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _context.Settings.Add(new Settings
                    {
                        SettingKey = kvp.Key,
                        SettingValue = kvp.Value,
                        Description = GetSettingDescription(kvp.Key),
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Clear cache
            _cachedSettings = null;

            _logger.LogInformation("SMTP settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving SMTP settings");
            throw;
        }
    }


    /// <summary>
    /// Gets description for a setting key.
    /// </summary>
    private string GetSettingDescription(string key)
    {
        return key switch
        {
            "smtp_host" => "SMTP Server Host",
            "smtp_port" => "SMTP Server Port",
            "smtp_username" => "SMTP Username",
            "smtp_password" => "SMTP Password (encrypted)",
            "smtp_use_ssl" => "Use SSL/TLS for SMTP",
            "email_from_name" => "Email Sender Name",
            "email_from_address" => "Email Sender Address",
            "smtp_enabled" => "Enable/Disable SMTP",
            _ => "Application Setting"
        };
    }

    /// <summary>
    /// Validates email address format.
    /// </summary>
    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Sends an email with retry logic for improved reliability.
    /// </summary>
    /// <param name="to">Recipient email address.</param>
    /// <param name="subject">Email subject.</param>
    /// <param name="body">Email body content.</param>
    /// <param name="isHtml">Whether the body is HTML.</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3).</param>
    /// <param name="delaySeconds">Delay between retries in seconds (default: 5).</param>
    /// <returns>True if email was sent successfully, false otherwise.</returns>
    public async Task<bool> SendEmailWithRetryAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        int maxRetries = 3,
        int delaySeconds = 5)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt < maxRetries)
        {
            attempt++;
            _logger.LogInformation($"Attempting to send email to {to} (Attempt {attempt}/{maxRetries})");

            try
            {
                var result = await SendEmailAsync(to, subject, body, isHtml);
                if (result)
                {
                    _logger.LogInformation($"Email sent successfully to {to} on attempt {attempt}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, $"Failed to send email to {to} on attempt {attempt}/{maxRetries}");
            }

            // Wait before retrying (except on last attempt)
            if (attempt < maxRetries)
            {
                _logger.LogInformation($"Waiting {delaySeconds} seconds before retry...");
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }

        _logger.LogError(lastException, $"Failed to send email to {to} after {maxRetries} attempts");
        return false;
    }

    /// <summary>
    /// Sends a notification email when a shared link is created.
    /// </summary>
    /// <param name="recipientEmail">Email address of the recipient (usually the link creator).</param>
    /// <param name="recipientName">Name of the recipient.</param>
    /// <param name="documentName">Name of the shared document.</param>
    /// <param name="linkUrl">Full URL of the shared link.</param>
    /// <param name="expiresAt">Expiration date of the link.</param>
    /// <returns>True if email was sent successfully, false otherwise.</returns>
    public async Task<bool> SendSharedLinkCreatedNotificationAsync(
        string recipientEmail,
        string recipientName,
        string documentName,
        string linkUrl,
        DateTime expiresAt)
    {
        try
        {
            // Check if notifications are enabled
            var settings = await GetSmtpSettingsAsync();
            if (settings == null || !settings.Enabled || !settings.NotifyOnSharedLinkCreated)
            {
                _logger.LogInformation("Shared link created notifications are disabled. Skipping email.");
                return false;
            }

            var subject = "تم إنشاء رابط مشاركة جديد - نظام إدارة المستندات القانونية";
            
            // Get link password protection status from settings (if available)
            // For now, we'll assume it's not password protected
            var body = EmailTemplates.SharedLinkCreated(
                recipientName,
                documentName,
                linkUrl,
                expiresAt,
                isPasswordProtected: false);

            return await SendEmailWithRetryAsync(
                to: recipientEmail,
                subject: subject,
                body: body,
                isHtml: true,
                maxRetries: 3);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending shared link created notification to {recipientEmail}");
            return false;
        }
    }

    /// <summary>
    /// Sends a notification email when a shared link is accessed.
    /// </summary>
    /// <param name="recipientEmail">Email address of the recipient (usually the link creator).</param>
    /// <param name="recipientName">Name of the recipient.</param>
    /// <param name="documentName">Name of the shared document.</param>
    /// <param name="linkUrl">Full URL of the shared link.</param>
    /// <param name="accessCount">Current access count for the link.</param>
    /// <returns>True if email was sent successfully, false otherwise.</returns>
    public async Task<bool> SendSharedLinkAccessedNotificationAsync(
        string recipientEmail,
        string recipientName,
        string documentName,
        string linkUrl,
        int accessCount)
    {
        try
        {
            // Check if notifications are enabled
            var settings = await GetSmtpSettingsAsync();
            if (settings == null || !settings.Enabled || !settings.NotifyOnSharedLinkAccessed)
            {
                _logger.LogInformation("Shared link accessed notifications are disabled. Skipping email.");
                return false;
            }

            var subject = "تم الوصول إلى رابط المشاركة - نظام إدارة المستندات القانونية";
            var body = EmailTemplates.SharedLinkAccessed(
                recipientName,
                documentName,
                linkUrl,
                accessCount,
                DateTime.UtcNow);

            return await SendEmailWithRetryAsync(
                to: recipientEmail,
                subject: subject,
                body: body,
                isHtml: true,
                maxRetries: 2); // Fewer retries for access notifications
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending shared link accessed notification to {recipientEmail}");
            return false;
        }
    }
}

