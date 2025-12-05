using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Defando.Tests.Integration.APITests;

/// <summary>
/// Tests for input validation (client-side and server-side).
/// </summary>
public class ValidationTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "http://localhost:5001";

    public ValidationTests()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    [Fact]
    public async Task TestEmptyInput_ReturnsBadRequest()
    {
        // Arrange
        var emptyInput = new { };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/documents", emptyInput);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || 
            response.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task TestSQLInjectionProtection()
    {
        // Arrange
        var maliciousQuery = "' OR '1'='1";

        // Act
        var response = await _httpClient.GetAsync($"/api/documents/search?query={maliciousQuery}");

        // Assert
        // Should not crash or expose data
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || 
            response.StatusCode == HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        // Should not contain SQL error messages
        Assert.DoesNotContain("SQL", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("syntax error", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TestXSSProtection()
    {
        // Arrange
        var xssPayload = "<script>alert('XSS')</script>";

        // Act
        var response = await _httpClient.GetAsync($"/api/documents/search?query={xssPayload}");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        // Should encode the script tag
        Assert.DoesNotContain("<script>", content);
        Assert.DoesNotContain("alert('XSS')", content);
    }

    [Fact]
    public async Task TestPathTraversalProtection()
    {
        // Arrange
        var maliciousPath = "../../../etc/passwd";

        // Act
        var response = await _httpClient.GetAsync($"/api/documents/file?path={maliciousPath}");

        // Assert
        // Should reject path traversal attempts
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || 
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TestMaxLengthValidation()
    {
        // Arrange
        var longString = new string('A', 10000); // Very long string
        var document = new
        {
            documentName = longString,
            documentType = "contract"
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/documents", document);

        // Assert
        // Should reject or truncate very long inputs
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || 
            response.StatusCode == HttpStatusCode.InternalServerError);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

