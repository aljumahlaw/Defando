namespace Defando.Services;

/// <summary>
/// Service interface for file storage operations.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves a file to storage and returns the file path.
    /// </summary>
    Task<string> SaveFileAsync(Stream fileStream, string fileName);

    /// <summary>
    /// Retrieves a file from storage as a stream.
    /// </summary>
    Task<Stream> GetFileAsync(string filePath);

    /// <summary>
    /// Deletes a file from storage.
    /// </summary>
    Task DeleteFileAsync(string filePath);

    /// <summary>
    /// Checks if a file exists in storage.
    /// </summary>
    Task<bool> FileExistsAsync(string filePath);

    /// <summary>
    /// Gets the size of a file in bytes.
    /// </summary>
    Task<long> GetFileSizeAsync(string filePath);

    /// <summary>
    /// Generates a unique file path based on GUID and date structure.
    /// </summary>
    string GenerateFilePath(string originalFileName);

    /// <summary>
    /// Checks if a file extension is allowed.
    /// </summary>
    bool IsExtensionAllowed(string extension);
}

