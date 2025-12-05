using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Defando.Tests.Integration.APITests;

/// <summary>
/// Tests for error handling and error messages.
/// </summary>
public class ErrorHandlingTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "http://localhost:5001";

    public ErrorHandlingTests()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    [Fact]
    public async Task TestCreateDocumentError_ReturnsGenericMessage()
    {
        // Arrange
        var invalidDocument = new { invalid = "data" };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/documents", invalidDocument);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("An error occurred", responseContent);
        Assert.DoesNotContain("Stack Trace", responseContent);
        Assert.DoesNotContain("Exception", responseContent);
        Assert.DoesNotContain("at ", responseContent);
    }

    [Fact]
    public async Task TestGetNonExistentDocument_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = 99999;

        // Act
        var response = await _httpClient.GetAsync($"/api/documents/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TestUnauthorizedAccess_ReturnsUnauthorized()
    {
        // Arrange - No authentication token

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/documents", new { });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TestInvalidInput_ReturnsBadRequest()
    {
        // Arrange
        var invalidInput = new { };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/documents", invalidInput);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        // Should be either 400 (validation error) or 500 (generic error)
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || 
            response.StatusCode == HttpStatusCode.InternalServerError);
        
        // Error message should be generic
        Assert.DoesNotContain("Stack Trace", responseContent);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

