using System.Net.Http.Json;
using System.Text.Json;
using DNFileRAG.Core.Configuration;
using DNFileRAG.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DNFileRAG.Infrastructure.Llm;

/// <summary>
/// LLM provider implementation using OpenAI's chat completions API.
/// </summary>
public class OpenAILlmProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly OpenAILlmOptions _options;
    private readonly ILogger<OpenAILlmProvider> _logger;

    public string ModelId => _options.Model;

    public OpenAILlmProvider(
        HttpClient httpClient,
        IOptions<OpenAILlmOptions> options,
        ILogger<OpenAILlmProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.openai.com/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
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

        _logger.LogDebug("Generating response with OpenAI model {Model}", _options.Model);

        var request = new
        {
            model = _options.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = options.Temperature,
            max_tokens = options.MaxTokens
        };

        var response = await _httpClient.PostAsJsonAsync(
            "v1/chat/completions",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAIResponse>(cancellationToken: cancellationToken);

        if (result?.Choices == null || result.Choices.Length == 0)
        {
            throw new InvalidOperationException("OpenAI returned no choices");
        }

        var text = result.Choices[0].Message.Content;

        _logger.LogDebug("OpenAI generated {Length} characters", text?.Length ?? 0);

        return text ?? string.Empty;
    }

    private class OpenAIResponse
    {
        public Choice[] Choices { get; set; } = [];
    }

    private class Choice
    {
        public Message Message { get; set; } = new();
    }

    private class Message
    {
        public string Content { get; set; } = string.Empty;
    }
}
