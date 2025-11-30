using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace LegalDocSystem.Tests.Integration.APITests;

/// <summary>
/// Performance tests for API endpoints.
/// </summary>
public class PerformanceTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "http://localhost:5001";

    public PerformanceTests()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    [Fact]
    public async Task TestGetDocumentsResponseTime()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _httpClient.GetAsync("/api/documents");
        stopwatch.Stop();

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 500, 
            $"Response time {stopwatch.ElapsedMilliseconds}ms exceeds 500ms threshold");
    }

    [Fact]
    public async Task TestGetDocumentByIdResponseTime()
    {
        // Arrange
        var documentId = 1;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _httpClient.GetAsync($"/api/documents/{documentId}");
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 200, 
            $"Response time {stopwatch.ElapsedMilliseconds}ms exceeds 200ms threshold");
    }

    [Fact]
    public async Task TestSearchDocumentsResponseTime()
    {
        // Arrange
        var query = "test";
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _httpClient.GetAsync($"/api/documents/search?query={query}");
        stopwatch.Stop();

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Response time {stopwatch.ElapsedMilliseconds}ms exceeds 1000ms threshold");
    }

    [Fact]
    public async Task TestConcurrentRequests()
    {
        // Arrange
        const int concurrentRequests = 10;
        var tasks = new Task<HttpResponseMessage>[concurrentRequests];
        var stopwatch = Stopwatch.StartNew();

        // Act - Send concurrent requests
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks[i] = _httpClient.GetAsync("/api/documents");
        }

        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        foreach (var response in responses)
        {
            Assert.True(response.IsSuccessStatusCode);
        }

        // All requests should complete within reasonable time
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
            $"Concurrent requests took {stopwatch.ElapsedMilliseconds}ms, exceeds 5000ms threshold");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

