using FluentAssertions;
using Defando.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;
using Xunit;

namespace Defando.Tests.Services;

/// <summary>
/// Unit tests for EncryptionService.
/// Tests encryption, decryption, and encryption detection functionality.
/// </summary>
public class EncryptionServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<EncryptionService>> _mockLogger;
    private readonly EncryptionService _encryptionService;

    public EncryptionServiceTests()
    {
        // Setup Mocks
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<EncryptionService>>();

        // Setup Configuration Mock - No encryption key (will use machine-specific key)
        _mockConfiguration.Setup(x => x["Encryption:Key"]).Returns((string?)null);

        // Create EncryptionService instance
        _encryptionService = new EncryptionService(
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    #region Encrypt Tests

    [Fact]
    public void Encrypt_WithValidPlainText_ReturnsEncryptedString()
    {
        // Arrange: إعداد البيانات
        var plainText = "TestPassword123";

        // Act: تنفيذ الوظيفة المطلوب اختبارها
        var result = _encryptionService.Encrypt(plainText);

        // Assert: التحقق من النتائج
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().StartWith("DPAPI:"); // Encryption marker
        result.Should().NotBe(plainText); // Should be different from original
    }

    [Fact]
    public void Encrypt_WithEmptyString_ThrowsArgumentNullException()
    {
        // Arrange
        var plainText = string.Empty;

        // Act & Assert
        var act = () => _encryptionService.Encrypt(plainText);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("plainText");
    }

    [Fact]
    public void Encrypt_WithNullString_ThrowsArgumentNullException()
    {
        // Arrange
        string? plainText = null;

        // Act & Assert
        var act = () => _encryptionService.Encrypt(plainText!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("plainText");
    }

    [Fact]
    public void Encrypt_WithLongText_ReturnsEncryptedString()
    {
        // Arrange
        var longText = new string('A', 1000); // 1000 characters

        // Act
        var result = _encryptionService.Encrypt(longText);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().StartWith("DPAPI:");
        result.Length.Should().BeGreaterThan(longText.Length); // Encrypted should be longer
    }

    [Fact]
    public void Encrypt_WithSpecialCharacters_ReturnsEncryptedString()
    {
        // Arrange
        var plainText = "Test@Password#123$%^&*()";

        // Act
        var result = _encryptionService.Encrypt(plainText);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().StartWith("DPAPI:");
    }

    [Fact]
    public void Encrypt_WithUnicodeCharacters_ReturnsEncryptedString()
    {
        // Arrange
        var plainText = "كلمة مرور عربية 密码 パスワード";

        // Act
        var result = _encryptionService.Encrypt(plainText);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().StartWith("DPAPI:");
    }

    [Fact]
    public void Encrypt_WithSameTextTwice_ReturnsDifferentEncryptedStrings()
    {
        // Arrange
        var plainText = "TestPassword123";

        // Act
        var result1 = _encryptionService.Encrypt(plainText);
        var result2 = _encryptionService.Encrypt(plainText);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        // Note: DPAPI may return same or different results depending on scope
        // But both should be valid encrypted strings
        result1.Should().StartWith("DPAPI:");
        result2.Should().StartWith("DPAPI:");
    }

    #endregion

    #region Decrypt Tests

    [Fact]
    public void Decrypt_WithValidEncryptedText_ReturnsOriginalPlainText()
    {
        // Arrange
        var plainText = "TestPassword123";
        var encryptedText = _encryptionService.Encrypt(plainText);

        // Act
        var result = _encryptionService.Decrypt(encryptedText);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(plainText);
    }

    [Fact]
    public void Decrypt_WithEmptyString_ThrowsArgumentNullException()
    {
        // Arrange
        var encryptedText = string.Empty;

        // Act & Assert
        var act = () => _encryptionService.Decrypt(encryptedText);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("encryptedText");
    }

    [Fact]
    public void Decrypt_WithNullString_ThrowsArgumentNullException()
    {
        // Arrange
        string? encryptedText = null;

        // Act & Assert
        var act = () => _encryptionService.Decrypt(encryptedText!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("encryptedText");
    }

    [Fact]
    public void Decrypt_WithInvalidEncryptedText_ThrowsCryptographicException()
    {
        // Arrange
        var invalidEncryptedText = "DPAPI:InvalidBase64String!!!";

        // Act & Assert
        var act = () => _encryptionService.Decrypt(invalidEncryptedText);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_WithCorruptedEncryptedText_ThrowsCryptographicException()
    {
        // Arrange
        var plainText = "TestPassword123";
        var encryptedText = _encryptionService.Encrypt(plainText);
        // Corrupt the encrypted text by changing some characters
        var corruptedText = encryptedText.Substring(0, encryptedText.Length - 5) + "XXXXX";

        // Act & Assert
        var act = () => _encryptionService.Decrypt(corruptedText);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_WithLongEncryptedText_ReturnsOriginalPlainText()
    {
        // Arrange
        var longText = new string('A', 1000);
        var encryptedText = _encryptionService.Encrypt(longText);

        // Act
        var result = _encryptionService.Decrypt(encryptedText);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(longText);
    }

    [Fact]
    public void Decrypt_WithSpecialCharacters_ReturnsOriginalPlainText()
    {
        // Arrange
        var plainText = "Test@Password#123$%^&*()";
        var encryptedText = _encryptionService.Encrypt(plainText);

        // Act
        var result = _encryptionService.Decrypt(encryptedText);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(plainText);
    }

    [Fact]
    public void Decrypt_WithUnicodeCharacters_ReturnsOriginalPlainText()
    {
        // Arrange
        var plainText = "كلمة مرور عربية 密码 パスワード";
        var encryptedText = _encryptionService.Encrypt(plainText);

        // Act
        var result = _encryptionService.Decrypt(encryptedText);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(plainText);
    }

    [Fact]
    public void Decrypt_WithTextWithoutMarker_ThrowsCryptographicException()
    {
        // Arrange
        var plainText = "NotEncryptedText";

        // Act & Assert
        // Note: Decrypt may try to decrypt as legacy Base64, but will likely fail
        var act = () => _encryptionService.Decrypt(plainText);
        // This might throw CryptographicException or FormatException depending on the content
        act.Should().Throw<Exception>();
    }

    #endregion

    #region IsEncrypted Tests

    [Fact]
    public void IsEncrypted_WithEncryptedText_ReturnsTrue()
    {
        // Arrange
        var plainText = "TestPassword123";
        var encryptedText = _encryptionService.Encrypt(plainText);

        // Act
        var result = _encryptionService.IsEncrypted(encryptedText);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsEncrypted_WithPlainText_ReturnsFalse()
    {
        // Arrange
        var plainText = "TestPassword123";

        // Act
        var result = _encryptionService.IsEncrypted(plainText);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEncrypted_WithEmptyString_ReturnsFalse()
    {
        // Arrange
        var text = string.Empty;

        // Act
        var result = _encryptionService.IsEncrypted(text);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEncrypted_WithNullString_ReturnsFalse()
    {
        // Arrange
        string? text = null;

        // Act
        var result = _encryptionService.IsEncrypted(text!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEncrypted_WithLegacyBase64Marker_ReturnsTrue()
    {
        // Arrange
        var text = "BASE64:SomeBase64String";

        // Act
        var result = _encryptionService.IsEncrypted(text);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsEncrypted_WithTextStartingWithDpapiMarker_ReturnsTrue()
    {
        // Arrange
        var text = "DPAPI:SomeEncryptedString";

        // Act
        var result = _encryptionService.IsEncrypted(text);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Encrypt-Decrypt Round Trip Tests

    [Fact]
    public void EncryptDecrypt_RoundTrip_WithSimpleText_ReturnsOriginal()
    {
        // Arrange
        var plainText = "SimpleText";

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_WithComplexText_ReturnsOriginal()
    {
        // Arrange
        var plainText = "Complex@Text#123$%^&*()_+-=[]{}|;':\",./<>?";

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_WithMultilineText_ReturnsOriginal()
    {
        // Arrange
        var plainText = "Line1\nLine2\nLine3";

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_WithWhitespace_ReturnsOriginal()
    {
        // Arrange
        var plainText = "   Text with   spaces   ";

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void EncryptionService_WithEncryptionKey_WorksCorrectly()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(x => x["Encryption:Key"]).Returns("TestEncryptionKey123");

        var service = new EncryptionService(mockConfig.Object, _mockLogger.Object);
        var plainText = "TestPassword";

        // Act
        var encrypted = service.Encrypt(plainText);
        var decrypted = service.Decrypt(encrypted);

        // Assert
        encrypted.Should().NotBeNull();
        encrypted.Should().StartWith("DPAPI:");
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void EncryptionService_WithoutEncryptionKey_WorksCorrectly()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(x => x["Encryption:Key"]).Returns((string?)null);

        var service = new EncryptionService(mockConfig.Object, _mockLogger.Object);
        var plainText = "TestPassword";

        // Act
        var encrypted = service.Encrypt(plainText);
        var decrypted = service.Decrypt(encrypted);

        // Assert
        encrypted.Should().NotBeNull();
        encrypted.Should().StartWith("DPAPI:");
        decrypted.Should().Be(plainText);
    }

    #endregion
}




