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

        // Sanitize text to remove problematic characters that can crash Ollama
        var sanitizedText = SanitizeTextForEmbedding(text);

        var request = new OllamaEmbeddingRequest
        {
            Model = _options.Model,
            Prompt = sanitizedText
        };

        _logger.LogDebug("Generating embedding using Ollama model {Model}", _options.Model);

        var response = await _httpClient.PostAsJsonAsync(
            _embeddingsEndpoint,
            request,
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Ollama API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
            throw new HttpRequestException($"Ollama API error: {response.StatusCode} - {errorContent}");
        }

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

    /// <summary>
    /// Sanitizes text to remove problematic characters that can crash Ollama's embedding model.
    /// PDFs often contain invisible control characters, zero-width spaces, and other characters
    /// that can cause issues with embedding models.
    /// </summary>
    private static string SanitizeTextForEmbedding(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sb = new System.Text.StringBuilder(text.Length);

        foreach (var c in text)
        {
            // Keep printable ASCII, standard whitespace, and common Unicode letters/numbers
            if (c >= 32 && c < 127) // Printable ASCII
            {
                sb.Append(c);
            }
            else if (c == '\n' || c == '\r' || c == '\t') // Standard whitespace
            {
                sb.Append(c);
            }
            else if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c))
            {
                // Keep Unicode letters, digits, punctuation (including Swedish å, ä, ö)
                sb.Append(c);
            }
            else if (char.IsWhiteSpace(c))
            {
                // Replace other whitespace (non-breaking space, etc.) with regular space
                sb.Append(' ');
            }
            // Skip control characters, zero-width characters, and other problematic chars
        }

        // Normalize multiple spaces to single space
        var result = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"[ ]+", " ");

        return result.Trim();
    }

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
