using System.Net;
using System.Net.Http.Json;
using DNFileRAG.Controllers;
using DNFileRAG.Core.Models;
using DNFileRAG.Models;
using DNFileRAG.Tests.Integration.Fixtures;
using FluentAssertions;
using Moq;

namespace DNFileRAG.Tests.Integration.Api;

/// <summary>
/// Integration tests for the Documents API endpoints.
/// </summary>
public class DocumentsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public DocumentsControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.SetupDefaultMocks();
        _client = _factory.CreateClient();
    }

    [Fact(Skip = "Requires appsettings.Testing.json configuration - TODO for future iteration")]
    public async Task GetDocuments_WithNoDocuments_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/documents");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DocumentListResponse>();
        result.Should().NotBeNull();
        result!.Documents.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact(Skip = "Requires appsettings.Testing.json configuration - TODO for future iteration")]
    public async Task GetDocuments_WithDocuments_ReturnsDocumentList()
    {
        // Arrange
        var documents = new List<DocumentInfo>
        {
            new()
            {
                FileId = "abc123",
                FilePath = "/documents/guide.pdf",
                FileName = "guide.pdf",
                LastIndexed = DateTime.UtcNow.AddHours(-1),
                ChunkCount = 15
            },
            new()
            {
                FileId = "def456",
                FilePath = "/documents/readme.md",
                FileName = "readme.md",
                LastIndexed = DateTime.UtcNow.AddMinutes(-30),
                ChunkCount = 3
            }
        };

        _factory.MockVectorStore
            .Setup(x => x.GetDocumentListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var response = await _client.GetAsync("/api/documents");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DocumentListResponse>();
        result.Should().NotBeNull();
        result!.Documents.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Documents[0].FileName.Should().Be("guide.pdf");
        result.Documents[0].ChunkCount.Should().Be(15);
    }

    [Fact(Skip = "Requires appsettings.Testing.json configuration - TODO for future iteration")]
    public async Task Reindex_ReturnsProcessedCount()
    {
        // Arrange
        _factory.MockIngestionPipeline
            .Setup(x => x.ReindexAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        // Act
        var response = await _client.PostAsync("/api/documents/reindex", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ReindexResponse>();
        result.Should().NotBeNull();
        result!.DocumentsProcessed.Should().Be(10);
    }

    [Fact(Skip = "Requires appsettings.Testing.json configuration - TODO for future iteration")]
    public async Task Reindex_CallsIngestionPipeline()
    {
        // Arrange
        _factory.MockIngestionPipeline
            .Setup(x => x.ReindexAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        await _client.PostAsync("/api/documents/reindex", null);

        // Assert
        _factory.MockIngestionPipeline.Verify(
            x => x.ReindexAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(Skip = "Requires appsettings.Testing.json configuration - TODO for future iteration")]
    public async Task DeleteDocument_WithValidPath_ReturnsDeletedCount()
    {
        // Arrange
        const string filePath = "/documents/guide.pdf";
        _factory.MockIngestionPipeline
            .Setup(x => x.RemoveFileAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(15);

        // Act
        var response = await _client.DeleteAsync($"/api/documents?filePath={Uri.EscapeDataString(filePath)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DeleteResponse>();
        result.Should().NotBeNull();
        result!.VectorsDeleted.Should().Be(15);
    }

    [Fact(Skip = "Requires appsettings.Testing.json configuration - TODO for future iteration")]
    public async Task DeleteDocument_WithEmptyPath_ReturnsBadRequest()
    {
        // Act
        var response = await _client.DeleteAsync("/api/documents?filePath=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Skip = "Requires appsettings.Testing.json configuration - TODO for future iteration")]
    public async Task DeleteDocument_WithNoPath_ReturnsBadRequest()
    {
        // Act
        var response = await _client.DeleteAsync("/api/documents");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Skip = "Requires appsettings.Testing.json configuration - TODO for future iteration")]
    public async Task DeleteDocument_CallsIngestionPipelineWithCorrectPath()
    {
        // Arrange
        const string filePath = "/documents/test.pdf";
        _factory.MockIngestionPipeline
            .Setup(x => x.RemoveFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        await _client.DeleteAsync($"/api/documents?filePath={Uri.EscapeDataString(filePath)}");

        // Assert
        _factory.MockIngestionPipeline.Verify(
            x => x.RemoveFileAsync(filePath, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(Skip = "Requires appsettings.Testing.json configuration - TODO for future iteration")]
    public async Task GetDocuments_ReturnsValidContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/documents");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }
}
