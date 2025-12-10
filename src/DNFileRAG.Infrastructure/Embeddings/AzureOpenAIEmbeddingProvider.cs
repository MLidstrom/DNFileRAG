using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DNFileRAG.Core.Configuration;
using DNFileRAG.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DNFileRAG.Infrastructure.Embeddings;

/// <summary>
/// Embedding provider using Azure OpenAI Service.
/// </summary>
public class AzureOpenAIEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly AzureOpenAIEmbeddingOptions _options;
    private readonly ILogger<AzureOpenAIEmbeddingProvider> _logger;
    private readonly string _embeddingsEndpoint;

    private const string ApiVersion = "2024-02-01";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AzureOpenAIEmbeddingProvider(
        HttpClient httpClient,
        IOptions<EmbeddingOptions> options,
        ILogger<AzureOpenAIEmbeddingProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value.AzureOpenAI;
        _logger = logger;

        ValidateConfiguration();
        _embeddingsEndpoint = BuildEndpoint();
        ConfigureHttpClient();
    }

    /// <inheritdoc />
    public int VectorDimension => 1536; // Azure OpenAI typically uses ada-002 compatible models

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

        var request = new AzureEmbeddingRequest
        {
            Input = texts.ToList()
        };

        _logger.LogDebug("Generating embeddings for {Count} texts using Azure OpenAI deployment {Deployment}",
            texts.Count, _options.DeploymentName);

        var response = await _httpClient.PostAsJsonAsync(
            _embeddingsEndpoint,
            request,
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AzureEmbeddingResponse>(JsonOptions, cancellationToken);

        if (result?.Data == null || result.Data.Count != texts.Count)
        {
            throw new InvalidOperationException("Invalid response from Azure OpenAI API");
        }

        _logger.LogDebug("Successfully generated {Count} embeddings from Azure OpenAI", result.Data.Count);

        return result.Data
            .OrderBy(d => d.Index)
            .Select(d => d.Embedding)
            .ToList();
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
            throw new InvalidOperationException("Azure OpenAI endpoint is not configured");

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Azure OpenAI API key is not configured");

        if (string.IsNullOrWhiteSpace(_options.DeploymentName))
            throw new InvalidOperationException("Azure OpenAI deployment name is not configured");
    }

    private string BuildEndpoint()
    {
        var baseUrl = _options.Endpoint.TrimEnd('/');
        return $"{baseUrl}/openai/deployments/{_options.DeploymentName}/embeddings?api-version={ApiVersion}";
    }

    private void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Add("api-key", _options.ApiKey);
    }

    #region Request/Response Models

    private class AzureEmbeddingRequest
    {
        public List<string> Input { get; set; } = [];
    }

    private class AzureEmbeddingResponse
    {
        public List<EmbeddingData> Data { get; set; } = [];
    }

    private class EmbeddingData
    {
        public int Index { get; set; }
        public float[] Embedding { get; set; } = [];
    }

    #endregion
}
