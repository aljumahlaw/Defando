using FluentAssertions;
using LegalDocSystem.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;

namespace LegalDocSystem.Tests.Services;

/// <summary>
/// Unit tests for FileStorageService.
/// Tests file storage operations, path validation, and security (Path Traversal prevention).
/// </summary>
public class FileStorageServiceTests : IDisposable
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<FileStorageService>> _mockLogger;
    private readonly FileStorageService _fileStorageService;
    private readonly string _testBasePath;

    public FileStorageServiceTests()
    {
        // Setup Mocks
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<FileStorageService>>();

        // Create a temporary directory for testing
        _testBasePath = Path.Combine(Path.GetTempPath(), "LegalDocSystem_TestStorage", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testBasePath);

        // Setup Configuration Mock
        _mockConfiguration.Setup(x => x["FileStorage:BasePath"]).Returns(_testBasePath);
        _mockConfiguration.Setup(x => x.GetValue<int>("FileStorage:MaxFileSizeMB", It.IsAny<int>())).Returns(100);
        
        var allowedExtensions = new[] { ".pdf", ".docx", ".jpg", ".png", ".jpeg", ".tiff", ".bmp" };
        var extensionsSection = new Mock<IConfigurationSection>();
        extensionsSection.Setup(x => x.Get<string[]>()).Returns(allowedExtensions);
        _mockConfiguration.Setup(x => x.GetSection("FileStorage:AllowedExtensions")).Returns(extensionsSection.Object);

        // Create FileStorageService instance
        _fileStorageService = new FileStorageService(
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        // Cleanup: Delete test directory
        try
        {
            if (Directory.Exists(_testBasePath))
            {
                Directory.Delete(_testBasePath, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    #region SaveFileAsync Tests

    [Fact]
    public async Task SaveFileAsync_WithValidFileName_SavesFileInSafePath()
    {
        // Arrange: إعداد البيانات
        var fileName = "test_document.pdf";
        var fileContent = "Test file content";
        using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

        // Act: تنفيذ الوظيفة المطلوب اختبارها
        var result = await _fileStorageService.SaveFileAsync(fileStream, fileName);

        // Assert: التحقق من النتائج
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        
        // Verify file was saved in safe path (within base path)
        var fullPath = Path.Combine(_testBasePath, result);
        var normalizedFullPath = Path.GetFullPath(fullPath);
        normalizedFullPath.Should().StartWith(Path.GetFullPath(_testBasePath));
        
        // Verify file exists
        File.Exists(normalizedFullPath).Should().BeTrue();
        
        // Verify file content
        var savedContent = await File.ReadAllTextAsync(normalizedFullPath);
        savedContent.Should().Be(fileContent);
    }

    [Fact]
    public async Task SaveFileAsync_WithPathTraversalInFileName_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var maliciousFileName = "../../../etc/passwd.pdf";
        var fileContent = "Malicious content";
        using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

        // Act & Assert
        var act = async () => await _fileStorageService.SaveFileAsync(fileStream, maliciousFileName);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Path traversal*");
    }

    [Fact]
    public async Task SaveFileAsync_WithBackslashesInFileName_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var maliciousFileName = "..\\..\\..\\windows\\system32\\config\\sam.pdf";
        var fileContent = "Malicious content";
        using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

        // Act & Assert
        var act = async () => await _fileStorageService.SaveFileAsync(fileStream, maliciousFileName);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Path traversal*");
    }

    [Fact]
    public async Task SaveFileAsync_WithAbsolutePathInFileName_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var maliciousFileName = "C:\\Windows\\System32\\config\\sam.pdf";
        var fileContent = "Malicious content";
        using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

        // Act & Assert
        var act = async () => await _fileStorageService.SaveFileAsync(fileStream, maliciousFileName);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Path traversal*");
    }

    [Fact]
    public async Task SaveFileAsync_WithInvalidExtension_ThrowsInvalidOperationException()
    {
        // Arrange
        var fileName = "test.exe"; // Not allowed extension
        var fileContent = "Test content";
        using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

        // Act & Assert
        var act = async () => await _fileStorageService.SaveFileAsync(fileStream, fileName);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not allowed*");
    }

    [Fact]
    public async Task SaveFileAsync_WithFileSizeExceedingLimit_ThrowsInvalidOperationException()
    {
        // Arrange
        var fileName = "large_file.pdf";
        var largeContent = new byte[101 * 1024 * 1024]; // 101 MB (exceeds 100 MB limit)
        using var fileStream = new MemoryStream(largeContent);

        // Act & Assert
        var act = async () => await _fileStorageService.SaveFileAsync(fileStream, fileName);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exceeds maximum*");
    }

    [Fact]
    public async Task SaveFileAsync_WithSpecialCharactersInFileName_SanitizesFileName()
    {
        // Arrange
        var fileName = "test<>:\"|?*file.pdf"; // Contains invalid file name characters
        var fileContent = "Test content";
        using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

        // Act
        var result = await _fileStorageService.SaveFileAsync(fileStream, fileName);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        
        // Verify file was saved (special characters should be sanitized)
        var fullPath = Path.Combine(_testBasePath, result);
        File.Exists(fullPath).Should().BeTrue();
    }

    #endregion

    #region GetFileAsync Tests

    [Fact]
    public async Task GetFileAsync_WithValidPath_ReturnsFileStream()
    {
        // Arrange
        var fileName = "test_document.pdf";
        var fileContent = "Test file content";
        using var saveStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        var savedPath = await _fileStorageService.SaveFileAsync(saveStream, fileName);

        // Act
        using var resultStream = await _fileStorageService.GetFileAsync(savedPath);
        using var reader = new StreamReader(resultStream);
        var content = await reader.ReadToEndAsync();

        // Assert
        content.Should().Be(fileContent);
    }

    [Fact]
    public async Task GetFileAsync_WithPathTraversal_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var maliciousPath = "../../../etc/passwd";

        // Act & Assert
        var act = async () => await _fileStorageService.GetFileAsync(maliciousPath);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Path traversal*");
    }

    [Fact]
    public async Task GetFileAsync_WithAbsolutePath_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var maliciousPath = "C:\\Windows\\System32\\config\\sam";

        // Act & Assert
        var act = async () => await _fileStorageService.GetFileAsync(maliciousPath);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Path traversal*");
    }

    [Fact]
    public async Task GetFileAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = "2025/01/15/nonexistent.pdf";

        // Act & Assert
        var act = async () => await _fileStorageService.GetFileAsync(nonExistentPath);
        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region DeleteFileAsync Tests

    [Fact]
    public async Task DeleteFileAsync_WithValidPath_DeletesFile()
    {
        // Arrange
        var fileName = "test_document.pdf";
        var fileContent = "Test file content";
        using var saveStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        var savedPath = await _fileStorageService.SaveFileAsync(saveStream, fileName);
        var fullPath = Path.Combine(_testBasePath, savedPath);
        
        // Verify file exists before deletion
        File.Exists(fullPath).Should().BeTrue();

        // Act
        await _fileStorageService.DeleteFileAsync(savedPath);

        // Assert
        File.Exists(fullPath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFileAsync_WithPathTraversal_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var maliciousPath = "../../../etc/passwd";

        // Act & Assert
        var act = async () => await _fileStorageService.DeleteFileAsync(maliciousPath);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Path traversal*");
    }

    [Fact]
    public async Task DeleteFileAsync_WithAbsolutePath_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var maliciousPath = "C:\\Windows\\System32\\config\\sam";

        // Act & Assert
        var act = async () => await _fileStorageService.DeleteFileAsync(maliciousPath);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Path traversal*");
    }

    [Fact]
    public async Task DeleteFileAsync_WithNonExistentFile_DoesNotThrow()
    {
        // Arrange
        var nonExistentPath = "2025/01/15/nonexistent.pdf";

        // Act
        var act = async () => await _fileStorageService.DeleteFileAsync(nonExistentPath);

        // Assert
        await act.Should().NotThrowAsync(); // Should handle gracefully
    }

    #endregion

    #region FileExistsAsync Tests

    [Fact]
    public async Task FileExistsAsync_WithExistingFile_ReturnsTrue()
    {
        // Arrange
        var fileName = "test_document.pdf";
        var fileContent = "Test file content";
        using var saveStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        var savedPath = await _fileStorageService.SaveFileAsync(saveStream, fileName);

        // Act
        var result = await _fileStorageService.FileExistsAsync(savedPath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task FileExistsAsync_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var nonExistentPath = "2025/01/15/nonexistent.pdf";

        // Act
        var result = await _fileStorageService.FileExistsAsync(nonExistentPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task FileExistsAsync_WithPathTraversal_ReturnsFalse()
    {
        // Arrange
        var maliciousPath = "../../../etc/passwd";

        // Act
        var result = await _fileStorageService.FileExistsAsync(maliciousPath);

        // Assert
        result.Should().BeFalse(); // Should return false without throwing
    }

    [Fact]
    public async Task FileExistsAsync_WithAbsolutePath_ReturnsFalse()
    {
        // Arrange
        var maliciousPath = "C:\\Windows\\System32\\config\\sam";

        // Act
        var result = await _fileStorageService.FileExistsAsync(maliciousPath);

        // Assert
        result.Should().BeFalse(); // Should return false without throwing
    }

    #endregion

    #region GetFileSizeAsync Tests

    [Fact]
    public async Task GetFileSizeAsync_WithExistingFile_ReturnsCorrectSize()
    {
        // Arrange
        var fileName = "test_document.pdf";
        var fileContent = "Test file content";
        using var saveStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        var savedPath = await _fileStorageService.SaveFileAsync(saveStream, fileName);

        // Act
        var result = await _fileStorageService.GetFileSizeAsync(savedPath);

        // Assert
        result.Should().Be(fileContent.Length);
    }

    [Fact]
    public async Task GetFileSizeAsync_WithPathTraversal_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var maliciousPath = "../../../etc/passwd";

        // Act & Assert
        var act = async () => await _fileStorageService.GetFileSizeAsync(maliciousPath);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Path traversal*");
    }

    [Fact]
    public async Task GetFileSizeAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = "2025/01/15/nonexistent.pdf";

        // Act & Assert
        var act = async () => await _fileStorageService.GetFileSizeAsync(nonExistentPath);
        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region GenerateFilePath Tests

    [Fact]
    public void GenerateFilePath_WithValidFileName_ReturnsSafeRelativePath()
    {
        // Arrange
        var fileName = "test_document.pdf";

        // Act
        var result = _fileStorageService.GenerateFilePath(fileName);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().NotContain("..");
        result.Should().NotStartWith("/");
        result.Should().NotStartWith("\\");
        result.Should().Contain(".pdf");
        // Should be in format: YYYY/MM/DD/{guid}.pdf
        result.Should().MatchRegex(@"^\d{4}/\d{2}/\d{2}/[a-f0-9-]+\.pdf$");
    }

    [Fact]
    public void GenerateFilePath_WithPathTraversalInFileName_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var maliciousFileName = "../../../etc/passwd.pdf";

        // Act & Assert
        var act = () => _fileStorageService.GenerateFilePath(maliciousFileName);
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*Path traversal*");
    }

    [Fact]
    public void GenerateFilePath_WithSpecialCharacters_SanitizesFileName()
    {
        // Arrange
        var fileName = "test<>:\"|?*file.pdf";

        // Act
        var result = _fileStorageService.GenerateFilePath(fileName);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotContain("..");
        result.Should().Contain(".pdf");
    }

    #endregion

    #region IsExtensionAllowed Tests

    [Fact]
    public void IsExtensionAllowed_WithAllowedExtension_ReturnsTrue()
    {
        // Arrange
        var fileName = "test.pdf";

        // Act
        var result = _fileStorageService.IsExtensionAllowed(fileName);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExtensionAllowed_WithDisallowedExtension_ReturnsFalse()
    {
        // Arrange
        var fileName = "test.exe";

        // Act
        var result = _fileStorageService.IsExtensionAllowed(fileName);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsExtensionAllowed_WithCaseInsensitiveExtension_ReturnsTrue()
    {
        // Arrange
        var fileName = "test.PDF"; // Uppercase

        // Act
        var result = _fileStorageService.IsExtensionAllowed(fileName);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region GetFullPath Tests

    [Fact]
    public void GetFullPath_WithValidRelativePath_ReturnsFullPathWithinBase()
    {
        // Arrange
        var relativePath = "2025/01/15/test.pdf";

        // Act
        var result = _fileStorageService.GetFullPath(relativePath);

        // Assert
        result.Should().NotBeNull();
        result.Should().StartWith(Path.GetFullPath(_testBasePath));
        result.Should().EndWith("test.pdf");
    }

    [Fact]
    public void GetFullPath_WithPathTraversal_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var maliciousPath = "../../../etc/passwd";

        // Act & Assert
        var act = () => _fileStorageService.GetFullPath(maliciousPath);
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*Path traversal*");
    }

    [Fact]
    public void GetFullPath_WithAbsolutePath_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var maliciousPath = "C:\\Windows\\System32\\config\\sam";

        // Act & Assert
        var act = () => _fileStorageService.GetFullPath(maliciousPath);
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*Path traversal*");
    }

    #endregion

    #region Path Traversal Edge Cases

    [Fact]
    public async Task SaveFileAsync_WithEncodedPathTraversal_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var maliciousFileName = "..%2F..%2F..%2Fetc%2Fpasswd.pdf"; // URL encoded
        var fileContent = "Malicious content";
        using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

        // Act & Assert
        var act = async () => await _fileStorageService.SaveFileAsync(fileStream, maliciousFileName);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Path traversal*");
    }

    [Fact]
    public async Task GetFileAsync_WithDoubleSlashes_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var maliciousPath = "..//..//..//etc//passwd";

        // Act & Assert
        var act = async () => await _fileStorageService.GetFileAsync(maliciousPath);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Path traversal*");
    }

    [Fact]
    public async Task GetFileAsync_WithMixedSlashes_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var maliciousPath = "..\\/../etc/passwd";

        // Act & Assert
        var act = async () => await _fileStorageService.GetFileAsync(maliciousPath);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Path traversal*");
    }

    [Fact]
    public async Task SaveFileAsync_WithNullBytesInPath_ThrowsException()
    {
        // Arrange
        var maliciousFileName = "test\0file.pdf"; // Contains null byte
        var fileContent = "Test content";
        using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

        // Act & Assert
        var act = async () => await _fileStorageService.SaveFileAsync(fileStream, maliciousFileName);
        await act.Should().ThrowAsync<Exception>(); // Should throw some exception
    }

    #endregion
}

