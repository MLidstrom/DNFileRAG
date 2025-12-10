using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DNFileRAG.Core.Configuration;
using DNFileRAG.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DNFileRAG.Infrastructure.Embeddings;

/// <summary>
/// Embedding provider using OpenAI's API.
/// </summary>
public class OpenAIEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly OpenAIEmbeddingOptions _options;
    private readonly ILogger<OpenAIEmbeddingProvider> _logger;
    private readonly int _vectorDimension;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OpenAIEmbeddingProvider(
        HttpClient httpClient,
        IOptions<EmbeddingOptions> options,
        ILogger<OpenAIEmbeddingProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value.OpenAI;
        _logger = logger;

        // Set vector dimension based on model
        _vectorDimension = GetVectorDimensionForModel(_options.Model);

        ConfigureHttpClient();
    }

    /// <inheritdoc />
    public int VectorDimension => _vectorDimension;

    /// <inheritdoc />
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        var embeddings = await GenerateEmbeddingsAsync([text], cancellationToken);
        return embeddings[0];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texts);

        if (texts.Count == 0)
            return [];

        var request = new OpenAIEmbeddingRequest
        {
            Input = texts.ToList(),
            Model = _options.Model
        };

        _logger.LogDebug("Generating embeddings for {Count} texts using model {Model}", texts.Count, _options.Model);

        var response = await _httpClient.PostAsJsonAsync(
            "https://api.openai.com/v1/embeddings",
            request,
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAIEmbeddingResponse>(JsonOptions, cancellationToken);

        if (result?.Data == null || result.Data.Count != texts.Count)
        {
            throw new InvalidOperationException("Invalid response from OpenAI API");
        }

        _logger.LogDebug("Successfully generated {Count} embeddings, tokens used: {Tokens}",
            result.Data.Count, result.Usage?.TotalTokens);

        return result.Data
            .OrderBy(d => d.Index)
            .Select(d => d.Embedding)
            .ToList();
    }

    private void ConfigureHttpClient()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured");
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    private static int GetVectorDimensionForModel(string model) => model.ToLowerInvariant() switch
    {
        "text-embedding-3-small" => 1536,
        "text-embedding-3-large" => 3072,
        "text-embedding-ada-002" => 1536,
        _ => 1536 // Default dimension
    };

    #region Request/Response Models

    private class OpenAIEmbeddingRequest
    {
        public List<string> Input { get; set; } = [];
        public string Model { get; set; } = string.Empty;
    }

    private class OpenAIEmbeddingResponse
    {
        public List<EmbeddingData> Data { get; set; } = [];
        public UsageInfo? Usage { get; set; }
    }

    private class EmbeddingData
    {
        public int Index { get; set; }
        public float[] Embedding { get; set; } = [];
    }

    private class UsageInfo
    {
        public int PromptTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    #endregion
}
