using System.Net.Http.Json;
using DNFileRAG.Core.Configuration;
using DNFileRAG.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DNFileRAG.Infrastructure.Llm;

/// <summary>
/// LLM provider implementation using Anthropic's messages API.
/// </summary>
public class AnthropicLlmProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly AnthropicLlmOptions _options;
    private readonly ILogger<AnthropicLlmProvider> _logger;

    private const string ApiVersion = "2023-06-01";

    public string ModelId => _options.Model;

    public AnthropicLlmProvider(
        HttpClient httpClient,
        IOptions<AnthropicLlmOptions> options,
        ILogger<AnthropicLlmProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.anthropic.com/");
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", ApiVersion);
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

        _logger.LogDebug("Generating response with Anthropic model {Model}", _options.Model);

        var request = new
        {
            model = _options.Model,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userPrompt }
            },
            temperature = options.Temperature,
            max_tokens = options.MaxTokens
        };

        var response = await _httpClient.PostAsJsonAsync(
            "v1/messages",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>(cancellationToken: cancellationToken);

        if (result?.Content == null || result.Content.Length == 0)
        {
            throw new InvalidOperationException("Anthropic returned no content");
        }

        var text = result.Content[0].Text;

        _logger.LogDebug("Anthropic generated {Length} characters", text?.Length ?? 0);

        return text ?? string.Empty;
    }

    private class AnthropicResponse
    {
        public ContentBlock[] Content { get; set; } = [];
    }

    private class ContentBlock
    {
        public string Type { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
