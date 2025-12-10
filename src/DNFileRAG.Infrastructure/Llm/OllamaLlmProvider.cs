using System.Net.Http.Json;
using DNFileRAG.Core.Configuration;
using DNFileRAG.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DNFileRAG.Infrastructure.Llm;

/// <summary>
/// LLM provider implementation using Ollama's chat API.
/// </summary>
public class OllamaLlmProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly OllamaLlmOptions _options;
    private readonly ILogger<OllamaLlmProvider> _logger;

    public string ModelId => _options.Model;

    public OllamaLlmProvider(
        HttpClient httpClient,
        IOptions<OllamaLlmOptions> options,
        ILogger<OllamaLlmProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        LlmGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(systemPrompt);
        ArgumentNullException.ThrowIfNull(userPrompt);

        options ??= new LlmGenerationOptions();

        _logger.LogDebug("Generating response with Ollama model {Model}", _options.Model);

        var request = new
        {
            model = _options.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            stream = false,
            options = new
            {
                temperature = options.Temperature,
                num_predict = options.MaxTokens
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            "api/chat",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: cancellationToken);

        if (result?.Message == null)
        {
            throw new InvalidOperationException("Ollama returned no message");
        }

        var text = result.Message.Content;

        _logger.LogDebug("Ollama generated {Length} characters", text?.Length ?? 0);

        return text ?? string.Empty;
    }

    private class OllamaChatResponse
    {
        public OllamaMessage? Message { get; set; }
    }

    private class OllamaMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
