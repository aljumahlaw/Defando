using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LegalDocSystem.Services;

/// <summary>
/// Service implementation for encryption and decryption using Windows DPAPI (Data Protection API).
/// Falls back to AES encryption on non-Windows platforms.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private const string EncryptionMarker = "DPAPI:";
    private const string LegacyBase64Marker = "BASE64:";
    private readonly ILogger<EncryptionService>? _logger;
    private readonly byte[]? _aesKey;

    public EncryptionService(IConfiguration configuration, ILogger<EncryptionService>? logger = null)
    {
        _logger = logger;
        
        // Get encryption key from environment variable or configuration (secure storage)
        var keyString = configuration["Encryption:Key"] 
            ?? Environment.GetEnvironmentVariable("LEGALDOC_ENCRYPTION_KEY");
        
        if (!string.IsNullOrEmpty(keyString))
        {
            using (var sha256 = SHA256.Create())
            {
                _aesKey = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
            }
            _logger?.LogInformation("AES encryption key loaded from secure configuration.");
        }
        else
        {
            _logger?.LogWarning("No encryption key configured. AES encryption will use machine-specific key (less secure). Consider setting LEGALDOC_ENCRYPTION_KEY environment variable.");
        }
    }

    /// <summary>
    /// Encrypts a plain text string using Windows DPAPI (Data Protection API).
    /// On non-Windows platforms, uses AES encryption with a machine-specific key.
    /// </summary>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentNullException(nameof(plainText));

        try
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes;

            // Use Windows DPAPI if available (Windows only)
            if (OperatingSystem.IsWindows())
            {
                // Use LocalMachine scope in production for shared access, CurrentUser in development
                var scope = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production"
                    ? DataProtectionScope.LocalMachine  // Shared across users (production)
                    : DataProtectionScope.CurrentUser;  // User-specific (development)
                
                encryptedBytes = ProtectedData.Protect(
                    plainBytes,
                    null, // Optional entropy (additional data for encryption)
                    scope);
                
                _logger?.LogDebug($"Password encrypted using Windows DPAPI ({scope} scope)");
            }
            else
            {
                // Fallback to AES encryption for non-Windows platforms
                encryptedBytes = EncryptWithAes(plainBytes);
                _logger?.LogDebug("Password encrypted using AES (non-Windows platform)");
            }

            // Return Base64-encoded encrypted data with marker
            string encryptedBase64 = Convert.ToBase64String(encryptedBytes);
            return EncryptionMarker + encryptedBase64;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error encrypting password");
            throw new CryptographicException("Failed to encrypt password", ex);
        }
    }

    /// <summary>
    /// Decrypts an encrypted string.
    /// Supports both DPAPI-encrypted strings and legacy Base64-encoded strings (for migration).
    /// </summary>
    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            throw new ArgumentNullException(nameof(encryptedText));

        try
        {
            // Check if it's a legacy Base64-encoded string (for backward compatibility)
            if (encryptedText.StartsWith(LegacyBase64Marker))
            {
                _logger?.LogWarning("Decrypting legacy Base64-encoded password. Consider re-encrypting with DPAPI.");
                return DecryptLegacyBase64(encryptedText);
            }

            // Check if it's a DPAPI-encrypted string
            if (!encryptedText.StartsWith(EncryptionMarker))
            {
                // Assume it's a legacy Base64 string without marker (for backward compatibility)
                _logger?.LogWarning("Decrypting password without encryption marker. Assuming legacy Base64 format.");
                return DecryptLegacyBase64(encryptedText);
            }

            // Extract the Base64-encoded encrypted data
            string encryptedBase64 = encryptedText.Substring(EncryptionMarker.Length);
            byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);
            byte[] decryptedBytes;

            // Use Windows DPAPI if available
            if (OperatingSystem.IsWindows())
            {
                // Try LocalMachine first (for production), then CurrentUser (for development/legacy)
                DataProtectionScope scope;
                try
                {
                    scope = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production"
                        ? DataProtectionScope.LocalMachine
                        : DataProtectionScope.CurrentUser;
                    
                    decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, scope);
                    _logger?.LogDebug($"Password decrypted using Windows DPAPI ({scope} scope)");
                }
                catch (CryptographicException)
                {
                    // Fallback: try the other scope if first attempt fails
                    scope = scope == DataProtectionScope.LocalMachine 
                        ? DataProtectionScope.CurrentUser 
                        : DataProtectionScope.LocalMachine;
                    decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, scope);
                    _logger?.LogDebug($"Password decrypted using Windows DPAPI ({scope} scope - fallback)");
                }
            }
            else
            {
                // Fallback to AES decryption for non-Windows platforms
                decryptedBytes = DecryptWithAes(encryptedBytes);
                _logger?.LogDebug("Password decrypted using AES");
            }

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (FormatException ex)
        {
            _logger?.LogError(ex, "Invalid Base64 format in encrypted password");
            throw new CryptographicException("Invalid encrypted password format", ex);
        }
        catch (CryptographicException ex)
        {
            _logger?.LogError(ex, "Cryptographic error decrypting password");
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error decrypting password");
            throw new CryptographicException("Failed to decrypt password", ex);
        }
    }

    /// <summary>
    /// Checks if a string is encrypted (starts with the encryption marker).
    /// </summary>
    public bool IsEncrypted(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        return text.StartsWith(EncryptionMarker) || text.StartsWith(LegacyBase64Marker);
    }

    /// <summary>
    /// Encrypts data using AES encryption (for non-Windows platforms).
    /// Uses a secure key from configuration or falls back to machine-specific key.
    /// </summary>
    private byte[] EncryptWithAes(byte[] plainBytes)
    {
        using (Aes aes = Aes.Create())
        {
            // Use configured key if available, otherwise fall back to machine-specific key
            if (_aesKey != null)
            {
                aes.Key = _aesKey;
            }
            else
            {
                // Fallback: Generate a machine-specific key from the machine name
                // This is less secure but provides backward compatibility
                string machineKey = Environment.MachineName + "LegalDocSystem2025";
                using (var sha256 = SHA256.Create())
                {
                    aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineKey));
                }
                _logger?.LogWarning("Using machine-specific encryption key. Consider setting LEGALDOC_ENCRYPTION_KEY environment variable for better security.");
            }

            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                byte[] encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                
                // Prepend IV to encrypted data
                byte[] result = new byte[aes.IV.Length + encrypted.Length];
                Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
                Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);
                
                return result;
            }
        }
    }

    /// <summary>
    /// Decrypts data using AES decryption (for non-Windows platforms).
    /// </summary>
    private byte[] DecryptWithAes(byte[] encryptedBytes)
    {
        using (Aes aes = Aes.Create())
        {
            // Use configured key if available, otherwise fall back to machine-specific key
            if (_aesKey != null)
            {
                aes.Key = _aesKey;
            }
            else
            {
                // Fallback: Generate the same machine-specific key
                string machineKey = Environment.MachineName + "LegalDocSystem2025";
                using (var sha256 = SHA256.Create())
                {
                    aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineKey));
                }
            }

            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Extract IV from the beginning of the encrypted data
            byte[] iv = new byte[aes.BlockSize / 8];
            byte[] cipherText = new byte[encryptedBytes.Length - iv.Length];
            Buffer.BlockCopy(encryptedBytes, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(encryptedBytes, iv.Length, cipherText, 0, cipherText.Length);
            aes.IV = iv;

            using (ICryptoTransform decryptor = aes.CreateDecryptor())
            {
                return decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
            }
        }
    }

    /// <summary>
    /// Decrypts a legacy Base64-encoded string (for backward compatibility during migration).
    /// </summary>
    private string DecryptLegacyBase64(string encryptedText)
    {
        string base64Text = encryptedText;
        if (base64Text.StartsWith(LegacyBase64Marker))
        {
            base64Text = base64Text.Substring(LegacyBase64Marker.Length);
        }

        byte[] bytes = Convert.FromBase64String(base64Text);
        return Encoding.UTF8.GetString(bytes);
    }
}

