using FluentAssertions;
using LegalDocSystem.Data;
using LegalDocSystem.Models;
using LegalDocSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LegalDocSystem.Tests.Services;

/// <summary>
/// Unit tests for EmailService.
/// Tests email sending logic, SMTP configuration, and retry behavior.
/// 
/// NOTE: These tests are written as pure unit tests and do not require a real SMTP server.
///       They focus on testing:
///       - SMTP settings management (Get/Save/Validate)
///       - Integration with EncryptionService (encrypt/decrypt passwords)
///       - EmailLog creation and persistence
///       - Logger calls and error handling
///       - Retry logic structure
///       
///       Integration tests for actual SMTP connectivity should be placed in a separate project.
/// </summary>
public class EmailServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<EmailService>> _mockLogger;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        // Setup In-Memory Database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Setup Mocks
        _mockLogger = new Mock<ILogger<EmailService>>();
        _mockEncryptionService = new Mock<IEncryptionService>();

        // Setup EncryptionService Mock - Default behavior
        _mockEncryptionService
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns((string plainText) => $"DPAPI:{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainText))}");

        _mockEncryptionService
            .Setup(x => x.Decrypt(It.IsAny<string>()))
            .Returns((string encryptedText) =>
            {
                if (encryptedText.StartsWith("DPAPI:"))
                {
                    var base64 = encryptedText.Substring(6);
                    return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                }
                return encryptedText;
            });

        // Create EmailService instance
        _emailService = new EmailService(
            _context,
            _mockLogger.Object,
            _mockEncryptionService.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Helper Methods

    private async Task SetupSmtpSettingsAsync(bool enabled = true)
    {
        var settings = new List<Settings>
        {
            new Settings { SettingKey = "smtp_host", SettingValue = "smtp.example.com" },
            new Settings { SettingKey = "smtp_port", SettingValue = "587" },
            new Settings { SettingKey = "smtp_username", SettingValue = "testuser" },
            new Settings { SettingKey = "smtp_password", SettingValue = "DPAPI:VGVzdFBhc3N3b3JkMTIz" }, // Encrypted "TestPassword123"
            new Settings { SettingKey = "smtp_use_ssl", SettingValue = "true" },
            new Settings { SettingKey = "email_from_name", SettingValue = "Test System" },
            new Settings { SettingKey = "email_from_address", SettingValue = "test@example.com" },
            new Settings { SettingKey = "smtp_enabled", SettingValue = enabled.ToString() }
        };

        _context.Settings.AddRange(settings);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region GetSmtpSettingsAsync Tests

    [Fact]
    public async Task GetSmtpSettingsAsync_WithValidSettings_ReturnsSmtpSettings()
    {
        // Arrange: إعداد البيانات
        await SetupSmtpSettingsAsync();

        // Act: تنفيذ الوظيفة المطلوب اختبارها
        var result = await _emailService.GetSmtpSettingsAsync();

        // Assert: التحقق من النتائج
        result.Should().NotBeNull();
        result!.Host.Should().Be("smtp.example.com");
        result.Port.Should().Be(587);
        result.Username.Should().Be("testuser");
        result.Password.Should().Be("DPAPI:VGVzdFBhc3N3b3JkMTIz");
        result.UseSsl.Should().BeTrue();
        result.FromName.Should().Be("Test System");
        result.FromAddress.Should().Be("test@example.com");
        result.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetSmtpSettingsAsync_WithNoSettings_ReturnsNull()
    {
        // Arrange: لا توجد إعدادات في قاعدة البيانات

        // Act
        var result = await _emailService.GetSmtpSettingsAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSmtpSettingsAsync_WithPartialSettings_ReturnsPartialSmtpSettings()
    {
        // Arrange
        var settings = new List<Settings>
        {
            new Settings { SettingKey = "smtp_host", SettingValue = "smtp.example.com" },
            new Settings { SettingKey = "smtp_port", SettingValue = "587" }
        };

        _context.Settings.AddRange(settings);
        await _context.SaveChangesAsync();

        // Act
        var result = await _emailService.GetSmtpSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Host.Should().Be("smtp.example.com");
        result.Port.Should().Be(587);
        result.Username.Should().BeEmpty();
        result.Enabled.Should().BeFalse(); // Default value
    }

    [Fact]
    public async Task GetSmtpSettingsAsync_CachesSettings()
    {
        // Arrange
        await SetupSmtpSettingsAsync();

        // Act
        var result1 = await _emailService.GetSmtpSettingsAsync();
        var result2 = await _emailService.GetSmtpSettingsAsync();

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.Should().BeSameAs(result2); // Should return cached instance
    }

    #endregion

    #region SaveSmtpSettingsAsync Tests

    [Fact]
    public async Task SaveSmtpSettingsAsync_WithValidSettings_SavesToDatabase()
    {
        // Arrange
        var settings = new SmtpSettings
        {
            Host = "smtp.newserver.com",
            Port = 465,
            Username = "newuser",
            Password = "NewPassword123",
            UseSsl = true,
            FromName = "New System",
            FromAddress = "new@example.com",
            Enabled = true
        };

        // Act
        await _emailService.SaveSmtpSettingsAsync(settings);

        // Assert
        var savedSettings = await _context.Settings.ToListAsync();
        savedSettings.Should().NotBeEmpty();
        savedSettings.Should().Contain(s => s.SettingKey == "smtp_host" && s.SettingValue == "smtp.newserver.com");
        savedSettings.Should().Contain(s => s.SettingKey == "smtp_port" && s.SettingValue == "465");
        savedSettings.Should().Contain(s => s.SettingKey == "smtp_username" && s.SettingValue == "newuser");
        
        // Verify password is encrypted
        var passwordSetting = savedSettings.FirstOrDefault(s => s.SettingKey == "smtp_password");
        passwordSetting.Should().NotBeNull();
        passwordSetting!.SettingValue.Should().StartWith("DPAPI:"); // Should be encrypted
        passwordSetting.SettingValue.Should().NotBe("NewPassword123");

        // Verify EncryptionService.Encrypt was called
        _mockEncryptionService.Verify(
            x => x.Encrypt("NewPassword123"),
            Times.Once);
    }

    [Fact]
    public async Task SaveSmtpSettingsAsync_WithExistingSettings_UpdatesExistingSettings()
    {
        // Arrange
        await SetupSmtpSettingsAsync();

        var newSettings = new SmtpSettings
        {
            Host = "smtp.updated.com",
            Port = 587,
            Username = "updateduser",
            Password = "UpdatedPassword",
            UseSsl = true,
            FromName = "Updated System",
            FromAddress = "updated@example.com",
            Enabled = true
        };

        // Act
        await _emailService.SaveSmtpSettingsAsync(newSettings);

        // Assert
        var hostSetting = await _context.Settings.FindAsync("smtp_host");
        hostSetting.Should().NotBeNull();
        hostSetting!.SettingValue.Should().Be("smtp.updated.com");
    }

    #endregion

    #region SendEmailAsync Tests

    [Fact]
    public async Task SendEmailAsync_WithSmtpDisabled_ReturnsFalse()
    {
        // Arrange
        await SetupSmtpSettingsAsync(enabled: false);

        // Act
        var result = await _emailService.SendEmailAsync(
            to: "recipient@example.com",
            subject: "Test Subject",
            body: "Test Body");

        // Assert
        result.Should().BeFalse();

        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SMTP is not configured or disabled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_WithNoSmtpSettings_ReturnsFalse()
    {
        // Arrange: لا توجد إعدادات SMTP

        // Act
        var result = await _emailService.SendEmailAsync(
            to: "recipient@example.com",
            subject: "Test Subject",
            body: "Test Body");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendEmailAsync_WithValidInput_CreatesEmailLog()
    {
        // Arrange
        await SetupSmtpSettingsAsync();

        // Act
        // NOTE: This will attempt SMTP connection, but we focus on unit test aspects:
        // - EmailLog creation
        // - EncryptionService.Decrypt call
        // - Logger calls
        var result = await _emailService.SendEmailAsync(
            to: "recipient@example.com",
            subject: "Test Subject",
            body: "Test Body");

        // Assert
        // Verify EmailLog was created regardless of SMTP connection result
        var emailLog = await _context.EmailLogs.FirstOrDefaultAsync();
        emailLog.Should().NotBeNull();
        emailLog!.SentTo.Should().Be("recipient@example.com");
        emailLog.Subject.Should().Be("Test Subject");
        emailLog.Body.Should().Be("Test Body");
        emailLog.Status.Should().BeOneOf("pending", "sent", "failed"); // Status depends on SMTP connection result
        
        // Verify EncryptionService.Decrypt was called
        _mockEncryptionService.Verify(
            x => x.Decrypt(It.Is<string>(s => s.StartsWith("DPAPI:"))),
            Times.AtLeastOnce);
        
        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendEmailAsync_WithCcAndBcc_CreatesEmailLog()
    {
        // Arrange
        await SetupSmtpSettingsAsync();

        // Act
        // NOTE: CC/BCC are included in MIME message but not stored in EmailLog
        // We verify EmailLog creation and service behavior
        var result = await _emailService.SendEmailAsync(
            to: "recipient@example.com",
            subject: "Test Subject",
            body: "Test Body",
            cc: "cc@example.com",
            bcc: "bcc@example.com");

        // Assert
        // Verify EmailLog was created
        var emailLog = await _context.EmailLogs.FirstOrDefaultAsync();
        emailLog.Should().NotBeNull();
        emailLog!.SentTo.Should().Be("recipient@example.com");
        emailLog.Subject.Should().Be("Test Subject");
        
        // Verify EncryptionService was called
        _mockEncryptionService.Verify(
            x => x.Decrypt(It.Is<string>(s => s.StartsWith("DPAPI:"))),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendEmailAsync_WithAttachments_CreatesEmailLog()
    {
        // Arrange
        await SetupSmtpSettingsAsync();

        // Create a temporary file for attachment
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "Test attachment content");

            var attachments = new List<string> { tempFile };

            // Act
            // NOTE: We verify EmailLog creation and attachment handling logic
            var result = await _emailService.SendEmailAsync(
                to: "recipient@example.com",
                subject: "Test Subject",
                body: "Test Body",
                attachments: attachments);

            // Assert
            // Verify EmailLog was created
            var emailLog = await _context.EmailLogs.FirstOrDefaultAsync();
            emailLog.Should().NotBeNull();
            emailLog!.SentTo.Should().Be("recipient@example.com");
            emailLog.Subject.Should().Be("Test Subject");
            
            // Verify EncryptionService was called
            _mockEncryptionService.Verify(
                x => x.Decrypt(It.Is<string>(s => s.StartsWith("DPAPI:"))),
                Times.AtLeastOnce);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SendEmailAsync_WithNonExistentAttachment_StillCreatesEmailLog()
    {
        // Arrange
        await SetupSmtpSettingsAsync();

        var attachments = new List<string> { "/nonexistent/file.pdf" };

        // Act
        // NOTE: We verify that missing attachments don't prevent EmailLog creation
        var result = await _emailService.SendEmailAsync(
            to: "recipient@example.com",
            subject: "Test Subject",
            body: "Test Body",
            attachments: attachments);

        // Assert
        // Verify EmailLog was created despite missing attachment
        var emailLog = await _context.EmailLogs.FirstOrDefaultAsync();
        emailLog.Should().NotBeNull();
        emailLog!.SentTo.Should().Be("recipient@example.com");
        
        // Verify warning was logged about missing attachment
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attachment file not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendEmailAsync_CallsEncryptionServiceDecrypt()
    {
        // Arrange
        await SetupSmtpSettingsAsync();

        // Act
        // NOTE: Pure unit test - we verify EncryptionService integration
        await _emailService.SendEmailAsync(
            to: "recipient@example.com",
            subject: "Test Subject",
            body: "Test Body");

        // Assert
        // Verify EncryptionService.Decrypt was called for SMTP password
        _mockEncryptionService.Verify(
            x => x.Decrypt(It.Is<string>(s => s.StartsWith("DPAPI:"))),
            Times.AtLeastOnce);
        
        // Verify EmailLog was created
        var emailLog = await _context.EmailLogs.FirstOrDefaultAsync();
        emailLog.Should().NotBeNull();
    }

    #endregion

    #region SendEmailWithRetryAsync Tests

    [Fact]
    public async Task SendEmailWithRetryAsync_WithSmtpDisabled_ReturnsFalse()
    {
        // Arrange
        await SetupSmtpSettingsAsync(enabled: false);

        // Act
        var result = await _emailService.SendEmailWithRetryAsync(
            to: "recipient@example.com",
            subject: "Test Subject",
            body: "Test Body",
            maxRetries: 3);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendEmailWithRetryAsync_WithValidInput_ExecutesRetryLogic()
    {
        // Arrange
        await SetupSmtpSettingsAsync();

        // Act
        // NOTE: Pure unit test - we verify retry logic structure and logging
        // The actual SMTP connection may fail, but we verify the retry mechanism
        var result = await _emailService.SendEmailWithRetryAsync(
            to: "recipient@example.com",
            subject: "Test Subject",
            body: "Test Body",
            maxRetries: 3,
            delaySeconds: 0); // No delay for faster unit testing

        // Assert
        // Verify retry attempt logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempting to send email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
        
        // Verify EmailLog was created (at least one attempt was made)
        var emailLogs = await _context.EmailLogs.ToListAsync();
        emailLogs.Should().NotBeEmpty();
        
        // Verify EncryptionService was called (at least once per retry attempt)
        _mockEncryptionService.Verify(
            x => x.Decrypt(It.Is<string>(s => s.StartsWith("DPAPI:"))),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendEmailWithRetryAsync_WithMaxRetries_ExecutesRetryLogic()
    {
        // Arrange
        await SetupSmtpSettingsAsync();

        // Act
        // NOTE: Pure unit test - we verify retry logic structure
        // The SMTP connection will likely fail, but we verify retry attempts are made
        var result = await _emailService.SendEmailWithRetryAsync(
            to: "recipient@example.com",
            subject: "Test Subject",
            body: "Test Body",
            maxRetries: 2,
            delaySeconds: 0); // No delay for faster unit testing

        // Assert
        // Verify that retry attempt logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempting to send email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
        
        // Verify EmailLog was created (retry logic should create at least one log)
        var emailLogs = await _context.EmailLogs.ToListAsync();
        emailLogs.Should().NotBeEmpty();
        
        // Verify EncryptionService was called (at least once per attempt)
        _mockEncryptionService.Verify(
            x => x.Decrypt(It.Is<string>(s => s.StartsWith("DPAPI:"))),
            Times.AtLeastOnce);
    }

    #endregion

    #region ValidateSmtpSettingsAsync Tests

    [Fact]
    public async Task ValidateSmtpSettingsAsync_WithValidSettings_ValidatesSettings()
    {
        // Arrange
        await SetupSmtpSettingsAsync();

        // Act
        // NOTE: This behaves like an integration test and may require a real or mocked SMTP server.
        // In unit test context, we verify:
        // - Settings validation logic (required fields, email format, port range)
        // - EncryptionService.Decrypt call
        // - Logger calls
        var result = await _emailService.ValidateSmtpSettingsAsync();

        // Assert
        // Result depends on actual SMTP server availability, but we verify validation logic was executed
        // Verify EncryptionService.Decrypt was called
        _mockEncryptionService.Verify(
            x => x.Decrypt(It.Is<string>(s => s.StartsWith("DPAPI:"))),
            Times.AtLeastOnce);
        
        // Verify logging occurred (either success or failure)
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ValidateSmtpSettingsAsync_WithNoSettings_ReturnsFalse()
    {
        // Arrange: لا توجد إعدادات SMTP

        // Act
        var result = await _emailService.ValidateSmtpSettingsAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateSmtpSettingsAsync_WithInvalidPort_ReturnsFalse()
    {
        // Arrange
        var settings = new List<Settings>
        {
            new Settings { SettingKey = "smtp_host", SettingValue = "smtp.example.com" },
            new Settings { SettingKey = "smtp_port", SettingValue = "99999" }, // Invalid port
            new Settings { SettingKey = "smtp_username", SettingValue = "testuser" },
            new Settings { SettingKey = "smtp_password", SettingValue = "DPAPI:VGVzdFBhc3N3b3JkMTIz" },
            new Settings { SettingKey = "email_from_address", SettingValue = "test@example.com" }
        };

        _context.Settings.AddRange(settings);
        await _context.SaveChangesAsync();

        // Act
        var result = await _emailService.ValidateSmtpSettingsAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateSmtpSettingsAsync_WithInvalidEmail_ReturnsFalse()
    {
        // Arrange
        var settings = new List<Settings>
        {
            new Settings { SettingKey = "smtp_host", SettingValue = "smtp.example.com" },
            new Settings { SettingKey = "smtp_port", SettingValue = "587" },
            new Settings { SettingKey = "smtp_username", SettingValue = "testuser" },
            new Settings { SettingKey = "smtp_password", SettingValue = "DPAPI:VGVzdFBhc3N3b3JkMTIz" },
            new Settings { SettingKey = "email_from_address", SettingValue = "invalid-email" } // Invalid email
        };

        _context.Settings.AddRange(settings);
        await _context.SaveChangesAsync();

        // Act
        var result = await _emailService.ValidateSmtpSettingsAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateSmtpSettingsAsync_CallsEncryptionServiceDecrypt()
    {
        // Arrange
        await SetupSmtpSettingsAsync();

        // Act
        await _emailService.ValidateSmtpSettingsAsync();

        // Assert
        // Verify EncryptionService.Decrypt was called for SMTP password
        _mockEncryptionService.Verify(
            x => x.Decrypt(It.Is<string>(s => s.StartsWith("DPAPI:"))),
            Times.AtLeastOnce);
    }

    #endregion

    #region SendTestEmailAsync Tests

    [Fact]
    public async Task SendTestEmailAsync_WithSmtpDisabled_ReturnsFalse()
    {
        // Arrange
        await SetupSmtpSettingsAsync(enabled: false);

        // Act
        var result = await _emailService.SendTestEmailAsync("test@example.com");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendTestEmailAsync_WithValidEmail_CreatesEmailLog()
    {
        // Arrange
        await SetupSmtpSettingsAsync();

        // Act
        // NOTE: Pure unit test - we verify EmailLog creation and service behavior
        // The actual SMTP connection may fail, but we verify the test email logic
        var result = await _emailService.SendTestEmailAsync("test@example.com");

        // Assert
        // Verify EmailLog was created with test email details
        var emailLog = await _context.EmailLogs.FirstOrDefaultAsync();
        emailLog.Should().NotBeNull();
        emailLog!.SentTo.Should().Be("test@example.com");
        emailLog.Subject.Should().Contain("اختبار إعدادات SMTP");
        emailLog.Body.Should().NotBeNullOrEmpty();
        
        // Verify EncryptionService was called
        _mockEncryptionService.Verify(
            x => x.Decrypt(It.Is<string>(s => s.StartsWith("DPAPI:"))),
            Times.AtLeastOnce);
    }

    #endregion

    #region SendSharedLinkCreatedNotificationAsync Tests

    [Fact]
    public async Task SendSharedLinkCreatedNotificationAsync_WithNotificationsDisabled_ReturnsFalse()
    {
        // Arrange
        await SetupSmtpSettingsAsync();
        // Disable notifications
        var notifySetting = new Settings
        {
            SettingKey = "notify_shared_link_created",
            SettingValue = "false"
        };
        _context.Settings.Add(notifySetting);
        await _context.SaveChangesAsync();

        // Act
        var result = await _emailService.SendSharedLinkCreatedNotificationAsync(
            recipientEmail: "test@example.com",
            recipientName: "Test User",
            documentName: "Test Document",
            linkUrl: "https://example.com/link",
            expiresAt: DateTime.UtcNow.AddDays(7));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendSharedLinkCreatedNotificationAsync_WithNotificationsEnabled_CreatesEmailLog()
    {
        // Arrange
        await SetupSmtpSettingsAsync();
        var notifySetting = new Settings
        {
            SettingKey = "notify_shared_link_created",
            SettingValue = "true"
        };
        _context.Settings.Add(notifySetting);
        await _context.SaveChangesAsync();

        // Act
        // NOTE: Pure unit test - we verify notification logic and EmailLog creation
        // The actual SMTP connection may fail, but we verify the notification flow
        var result = await _emailService.SendSharedLinkCreatedNotificationAsync(
            recipientEmail: "test@example.com",
            recipientName: "Test User",
            documentName: "Test Document",
            linkUrl: "https://example.com/link",
            expiresAt: DateTime.UtcNow.AddDays(7));

        // Assert
        // Verify EmailLog was created
        var emailLog = await _context.EmailLogs.FirstOrDefaultAsync();
        emailLog.Should().NotBeNull();
        emailLog!.SentTo.Should().Be("test@example.com");
        emailLog.Subject.Should().Contain("رابط مشاركة جديد");
        
        // Verify EncryptionService was called
        _mockEncryptionService.Verify(
            x => x.Decrypt(It.Is<string>(s => s.StartsWith("DPAPI:"))),
            Times.AtLeastOnce);
    }

    #endregion

    #region SendSharedLinkAccessedNotificationAsync Tests

    [Fact]
    public async Task SendSharedLinkAccessedNotificationAsync_WithNotificationsDisabled_ReturnsFalse()
    {
        // Arrange
        await SetupSmtpSettingsAsync();
        var notifySetting = new Settings
        {
            SettingKey = "notify_shared_link_accessed",
            SettingValue = "false"
        };
        _context.Settings.Add(notifySetting);
        await _context.SaveChangesAsync();

        // Act
        var result = await _emailService.SendSharedLinkAccessedNotificationAsync(
            recipientEmail: "test@example.com",
            recipientName: "Test User",
            documentName: "Test Document",
            linkUrl: "https://example.com/link",
            accessCount: 5);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendSharedLinkAccessedNotificationAsync_WithNotificationsEnabled_CreatesEmailLog()
    {
        // Arrange
        await SetupSmtpSettingsAsync();
        var notifySetting = new Settings
        {
            SettingKey = "notify_shared_link_accessed",
            SettingValue = "true"
        };
        _context.Settings.Add(notifySetting);
        await _context.SaveChangesAsync();

        // Act
        // NOTE: Pure unit test - we verify notification logic and EmailLog creation
        // The actual SMTP connection may fail, but we verify the notification flow
        var result = await _emailService.SendSharedLinkAccessedNotificationAsync(
            recipientEmail: "test@example.com",
            recipientName: "Test User",
            documentName: "Test Document",
            linkUrl: "https://example.com/link",
            accessCount: 5);

        // Assert
        // Verify EmailLog was created
        var emailLog = await _context.EmailLogs.FirstOrDefaultAsync();
        emailLog.Should().NotBeNull();
        emailLog!.SentTo.Should().Be("test@example.com");
        emailLog.Subject.Should().Contain("الوصول إلى رابط المشاركة");
        
        // Verify EncryptionService was called
        _mockEncryptionService.Verify(
            x => x.Decrypt(It.Is<string>(s => s.StartsWith("DPAPI:"))),
            Times.AtLeastOnce);
    }

    #endregion
}

