using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DNFileRAG.Core.Configuration;
using DNFileRAG.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DNFileRAG.Infrastructure.Embeddings;

/// <summary>
/// Embedding provider using local Ollama instance.
/// </summary>
public class OllamaEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly OllamaEmbeddingOptions _options;
    private readonly ILogger<OllamaEmbeddingProvider> _logger;
    private readonly string _embeddingsEndpoint;
    private int? _cachedDimension;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OllamaEmbeddingProvider(
        HttpClient httpClient,
        IOptions<EmbeddingOptions> options,
        ILogger<OllamaEmbeddingProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value.Ollama;
        _logger = logger;

        _embeddingsEndpoint = $"{_options.BaseUrl.TrimEnd('/')}/api/embeddings";
    }

    /// <inheritdoc />
    public int VectorDimension => _cachedDimension ?? GetDefaultDimensionForModel(_options.Model);

    /// <inheritdoc />
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        var request = new OllamaEmbeddingRequest
        {
            Model = _options.Model,
            Prompt = text
        };

        _logger.LogDebug("Generating embedding using Ollama model {Model}", _options.Model);

        var response = await _httpClient.PostAsJsonAsync(
            _embeddingsEndpoint,
            request,
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(JsonOptions, cancellationToken);

        if (result?.Embedding == null || result.Embedding.Length == 0)
        {
            throw new InvalidOperationException("Invalid response from Ollama API");
        }

        // Cache the dimension from first successful response
        _cachedDimension ??= result.Embedding.Length;

        _logger.LogDebug("Successfully generated embedding with dimension {Dimension}", result.Embedding.Length);

        return result.Embedding;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texts);

        if (texts.Count == 0)
            return [];

        // Ollama doesn't support batch embeddings natively, so we process sequentially
        // For better performance in production, consider parallel processing with rate limiting
        var embeddings = new List<float[]>(texts.Count);

        _logger.LogDebug("Generating {Count} embeddings sequentially using Ollama", texts.Count);

        foreach (var text in texts)
        {
            var embedding = await GenerateEmbeddingAsync(text, cancellationToken);
            embeddings.Add(embedding);
        }

        return embeddings;
    }

    private static int GetDefaultDimensionForModel(string model) => model.ToLowerInvariant() switch
    {
        "nomic-embed-text" => 768,
        "all-minilm" => 384,
        "mxbai-embed-large" => 1024,
        _ => 768 // Default for unknown models
    };

    #region Request/Response Models

    private class OllamaEmbeddingRequest
    {
        public string Model { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
    }

    private class OllamaEmbeddingResponse
    {
        public float[] Embedding { get; set; } = [];
    }

    #endregion
}
