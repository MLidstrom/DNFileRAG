using System.Net;
using System.Net.Http.Json;
using DNFileRAG.Core.Configuration;
using DNFileRAG.Infrastructure.Embeddings;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace DNFileRAG.Tests.Infrastructure.Embeddings;

public class OllamaEmbeddingProviderTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly IOptions<EmbeddingOptions> _options;

    public OllamaEmbeddingProviderTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandlerMock.Object);
        _options = Options.Create(new EmbeddingOptions
        {
            Provider = "Ollama",
            Ollama = new OllamaEmbeddingOptions
            {
                BaseUrl = "http://localhost:11434",
                Model = "nomic-embed-text"
            }
        });
    }

    private OllamaEmbeddingProvider CreateProvider()
    {
        return new OllamaEmbeddingProvider(_httpClient, _options, NullLogger<OllamaEmbeddingProvider>.Instance);
    }

    private void SetupMockResponse(object responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(responseBody)
        };

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldUseDefaultVectorDimensionForModel()
    {
        // Arrange & Act
        var provider = CreateProvider();

        // Assert - nomic-embed-text model has 768 dimensions by default
        provider.VectorDimension.Should().Be(768);
    }

    #endregion

    #region GenerateEmbeddingAsync Tests

    [Fact]
    public async Task GenerateEmbeddingAsync_WithNullText_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        var act = () => provider.GenerateEmbeddingAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithValidText_ShouldReturnEmbedding()
    {
        // Arrange
        var expectedEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        SetupMockResponse(new
        {
            embedding = expectedEmbedding
        });

        var provider = CreateProvider();

        // Act
        var result = await provider.GenerateEmbeddingAsync("test text");

        // Assert
        result.Should().BeEquivalentTo(expectedEmbedding);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithNullEmbedding_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupMockResponse(new { embedding = (float[]?)null });

        var provider = CreateProvider();

        // Act & Assert
        var act = () => provider.GenerateEmbeddingAsync("test text");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid response*");
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithApiError_ShouldThrowHttpRequestException()
    {
        // Arrange
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var provider = CreateProvider();

        // Act & Assert
        var act = () => provider.GenerateEmbeddingAsync("test text");
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    #endregion

    #region GenerateEmbeddingsAsync Tests

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithNullTexts_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        var act = () => provider.GenerateEmbeddingsAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var result = await provider.GenerateEmbeddingsAsync([]);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithMultipleTexts_ShouldReturnMultipleEmbeddings()
    {
        // Arrange - Ollama processes texts sequentially, so we need to set up responses for each call
        var embeddings = new[]
        {
            new float[] { 0.1f, 0.2f },
            new float[] { 0.3f, 0.4f }
        };

        var callCount = 0;
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                var idx = callCount++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { embedding = embeddings[idx % embeddings.Length] })
                };
            });

        var provider = CreateProvider();

        // Act
        var result = await provider.GenerateEmbeddingsAsync(["text1", "text2"]);

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().BeEquivalentTo(embeddings[0]);
        result[1].Should().BeEquivalentTo(embeddings[1]);
    }

    #endregion
}
