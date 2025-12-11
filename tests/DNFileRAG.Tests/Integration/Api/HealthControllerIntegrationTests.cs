using System.Net;
using System.Net.Http.Json;
using DNFileRAG.Controllers;
using DNFileRAG.Tests.Integration.Fixtures;
using FluentAssertions;

namespace DNFileRAG.Tests.Integration.Api;

/// <summary>
/// Integration tests for the Health API endpoints.
/// </summary>
public class HealthControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public HealthControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.SetupDefaultMocks();
        _client = _factory.CreateClient();
    }

    [Fact(Skip = "Requires appsettings.Testing.json configuration - TODO for future iteration")]
    public async Task GetHealth_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var health = await response.Content.ReadFromJsonAsync<HealthResponse>();
        health.Should().NotBeNull();
        health!.Status.Should().Be("Healthy");
        health.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact(Skip = "Requires appsettings.Testing.json configuration - TODO for future iteration")]
    public async Task GetDetailedHealth_ReturnsAllComponents()
    {
        // Act
        var response = await _client.GetAsync("/api/health/detailed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var health = await response.Content.ReadFromJsonAsync<DetailedHealthResponse>();
        health.Should().NotBeNull();
        health!.Status.Should().Be("Healthy");
        health.Components.Should().ContainKey("api");
        health.Components.Should().ContainKey("vectorStore");
        health.Components.Should().ContainKey("embeddingProvider");
        health.Components.Should().ContainKey("llmProvider");
        health.Components.Should().ContainKey("fileWatcher");
    }

    [Fact(Skip = "Requires appsettings.Testing.json configuration - TODO for future iteration")]
    public async Task GetHealth_ReturnsValidContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }
}
