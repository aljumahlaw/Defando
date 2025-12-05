namespace Defando.Services;

/// <summary>
/// Interface for encryption and decryption services.
/// Provides secure encryption/decryption for sensitive data like SMTP passwords.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a plain text string using secure encryption (DPAPI on Windows, AES on other platforms).
    /// </summary>
    /// <param name="plainText">The plain text to encrypt.</param>
    /// <returns>Encrypted string (Base64 encoded).</returns>
    /// <exception cref="ArgumentNullException">Thrown when plainText is null or empty.</exception>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts an encrypted string.
    /// </summary>
    /// <param name="encryptedText">The encrypted string (Base64 encoded).</param>
    /// <returns>Decrypted plain text string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when encryptedText is null or empty.</exception>
    /// <exception cref="CryptographicException">Thrown when decryption fails (invalid data or key).</exception>
    string Decrypt(string encryptedText);

    /// <summary>
    /// Checks if a string is encrypted (starts with the encryption marker).
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True if the text appears to be encrypted, false otherwise.</returns>
    bool IsEncrypted(string text);
}

