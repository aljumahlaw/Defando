using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LegalDocSystem.Services;

/// <summary>
/// Service implementation for file storage operations.
/// </summary>
public class FileStorageService : IFileStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _basePath;
    private readonly int _maxFileSizeMB;
    private readonly long _maxFileSizeBytes;
    private readonly string[] _allowedExtensions;

    public FileStorageService(IConfiguration configuration, ILogger<FileStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        _basePath = _configuration["FileStorage:BasePath"] ?? "D:\\LegalDMS\\Files";
        _maxFileSizeMB = _configuration.GetValue<int>("FileStorage:MaxFileSizeMB", 100);
        _maxFileSizeBytes = _maxFileSizeMB * 1024 * 1024; // Convert MB to bytes

        var extensions = _configuration.GetSection("FileStorage:AllowedExtensions").Get<string[]>();
        _allowedExtensions = extensions ?? new[] { ".pdf", ".docx", ".jpg", ".png", ".jpeg", ".tiff", ".bmp" };
    }

    /// <summary>
    /// Saves a file to storage and returns the file path.
    /// </summary>
    public async Task<string> SaveFileAsync(Stream fileStream, string fileName)
    {
        try
        {
            // Sanitize file name to prevent path traversal
            var sanitizedFileName = SanitizeFileName(fileName);
            
            // Validate file extension
            var fileExtension = Path.GetExtension(sanitizedFileName).ToLower();
            if (!_allowedExtensions.Contains(fileExtension))
            {
                throw new InvalidOperationException($"File extension {fileExtension} is not allowed.");
            }

            // Validate file size
            if (fileStream.Length > _maxFileSizeBytes)
            {
                throw new InvalidOperationException($"File size exceeds maximum allowed size of {_maxFileSizeMB} MB.");
            }

            // Generate unique file path (uses sanitized file name)
            var filePath = GenerateFilePath(sanitizedFileName);
            
            // Validate the generated path to ensure it's safe
            var fullPath = ValidateAndNormalizePath(filePath);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation($"Created directory: {directory}");
            }

            // Save file
            using (var fileStreamOut = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await fileStream.CopyToAsync(fileStreamOut);
            }

            _logger.LogInformation($"File saved successfully: {fullPath} (Size: {fileStream.Length} bytes)");

            // Return relative path for database storage
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving file: {fileName}");
            throw;
        }
    }

    /// <summary>
    /// Sanitizes a file name by removing path traversal characters and dangerous characters.
    /// </summary>
    /// <param name="fileName">The original file name from user input.</param>
    /// <returns>A sanitized file name safe for use.</returns>
    private string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));

        // Get only the file name (remove any path components)
        var safeFileName = Path.GetFileName(fileName);
        
        // Remove any remaining path traversal attempts
        if (safeFileName.Contains("..") || safeFileName.Contains("/") || safeFileName.Contains("\\"))
        {
            _logger.LogWarning($"Path traversal attempt detected in file name: {fileName}");
            throw new UnauthorizedAccessException("Invalid file name. Path traversal characters detected.");
        }

        // Remove or replace dangerous characters
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var invalidChar in invalidChars)
        {
            safeFileName = safeFileName.Replace(invalidChar, '_');
        }

        // Ensure file name is not empty after sanitization
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            throw new ArgumentException("File name is invalid after sanitization.", nameof(fileName));
        }

        return safeFileName;
    }

    /// <summary>
    /// Validates that a file path is within the base path (prevents path traversal attacks).
    /// </summary>
    /// <param name="filePath">The relative file path to validate.</param>
    /// <returns>The normalized full path if valid.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if path traversal is detected.</exception>
    private string ValidateAndNormalizePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));

        // Check for path traversal patterns before normalization
        if (filePath.Contains("..") || filePath.Contains("//") || filePath.Contains("\\\\"))
        {
            _logger.LogWarning($"Path traversal attempt detected in path: {filePath}");
            throw new UnauthorizedAccessException("Invalid file path. Path traversal detected.");
        }

        // Remove any leading/trailing slashes and normalize
        filePath = filePath.TrimStart('/', '\\').TrimEnd('/', '\\');

        // Normalize base path (remove trailing separators)
        var basePathNormalized = Path.GetFullPath(_basePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        
        // Combine and normalize the full path
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, filePath));

        // Prevent path traversal attacks - ensure the resolved path is within base path
        if (!fullPath.StartsWith(basePathNormalized, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning($"Path traversal attempt detected. Base: {basePathNormalized}, Requested: {fullPath}, Input: {filePath}");
            throw new UnauthorizedAccessException("Invalid file path. Path traversal detected.");
        }

        return fullPath;
    }

    /// <summary>
    /// Retrieves a file from storage as a stream.
    /// </summary>
    public async Task<Stream> GetFileAsync(string filePath)
    {
        try
        {
            // Validate and normalize path to prevent path traversal
            var fullPath = ValidateAndNormalizePath(filePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _logger.LogInformation($"File retrieved: {fullPath}");

            return fileStream;
        }
        catch (UnauthorizedAccessException)
        {
            // Re-throw path traversal exceptions without logging sensitive details
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving file: {filePath}");
            throw;
        }
    }

    /// <summary>
    /// Deletes a file from storage.
    /// </summary>
    public async Task DeleteFileAsync(string filePath)
    {
        try
        {
            // Validate and normalize path to prevent path traversal
            var fullPath = ValidateAndNormalizePath(filePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation($"File deleted: {fullPath}");

                // TODO: Optionally delete empty directories
                // CleanupEmptyDirectories(Path.GetDirectoryName(fullPath));
            }
            else
            {
                _logger.LogWarning($"File not found for deletion: {fullPath}");
            }

            await Task.CompletedTask;
        }
        catch (UnauthorizedAccessException)
        {
            // Re-throw path traversal exceptions without logging sensitive details
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting file: {filePath}");
            throw;
        }
    }

    /// <summary>
    /// Checks if a file exists in storage.
    /// </summary>
    public async Task<bool> FileExistsAsync(string filePath)
    {
        try
        {
            // Validate and normalize path to prevent path traversal
            var fullPath = ValidateAndNormalizePath(filePath);
            return await Task.FromResult(File.Exists(fullPath));
        }
        catch (UnauthorizedAccessException)
        {
            // Path traversal attempt - return false without logging
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking file existence: {filePath}");
            return false;
        }
    }

    /// <summary>
    /// Gets the size of a file in bytes.
    /// </summary>
    public async Task<long> GetFileSizeAsync(string filePath)
    {
        try
        {
            // Validate and normalize path to prevent path traversal
            var fullPath = ValidateAndNormalizePath(filePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var fileInfo = new FileInfo(fullPath);
            return await Task.FromResult(fileInfo.Length);
        }
        catch (UnauthorizedAccessException)
        {
            // Re-throw path traversal exceptions without logging sensitive details
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting file size: {filePath}");
            throw;
        }
    }

    /// <summary>
    /// Generates a unique file path based on GUID and date structure (Year/Month/Day).
    /// </summary>
    /// <param name="originalFileName">The original file name (should be sanitized before calling this method).</param>
    /// <returns>A relative file path safe for storage.</returns>
    public string GenerateFilePath(string originalFileName)
    {
        // Check for path traversal in original file name
        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("File name cannot be empty.", nameof(originalFileName));

        // Extract only the file name (remove any path components)
        var safeFileName = Path.GetFileName(originalFileName);
        
        // Check for path traversal attempts
        if (safeFileName.Contains("..") || safeFileName.Contains("/") || safeFileName.Contains("\\"))
        {
            _logger.LogWarning($"Path traversal attempt detected in file name: {originalFileName}");
            throw new UnauthorizedAccessException("Invalid file name. Path traversal characters detected.");
        }

        // Get extension from safe file name
        var fileExtension = Path.GetExtension(safeFileName);
        
        // Validate extension is not empty
        if (string.IsNullOrEmpty(fileExtension))
        {
            throw new ArgumentException("File name must have an extension.", nameof(originalFileName));
        }
        
        // Generate unique file name using GUID
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

        // Organize files by date: Year/Month/Day
        var now = DateTime.UtcNow;
        var datePath = Path.Combine(now.Year.ToString(), now.Month.ToString("00"), now.Day.ToString("00"));

        // Combine and normalize path separators
        var relativePath = Path.Combine(datePath, uniqueFileName).Replace("\\", "/");
        
        // Final validation: ensure no path traversal in generated path
        if (relativePath.Contains("..") || relativePath.StartsWith("/") || relativePath.StartsWith("\\"))
        {
            _logger.LogError($"Generated path contains dangerous characters: {relativePath}");
            throw new InvalidOperationException("Generated file path is invalid.");
        }

        return relativePath;
    }

    /// <summary>
    /// Validates file extension against allowed extensions.
    /// </summary>
    public bool IsExtensionAllowed(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        return _allowedExtensions.Contains(extension);
    }

    /// <summary>
    /// Gets the full physical path for a relative file path.
    /// </summary>
    public string GetFullPath(string relativePath)
    {
        // Validate and normalize path to prevent path traversal
        return ValidateAndNormalizePath(relativePath);
    }
}

