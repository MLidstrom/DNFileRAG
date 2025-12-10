using System.Net.Http.Json;
using DNFileRAG.Core.Configuration;
using DNFileRAG.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DNFileRAG.Infrastructure.Llm;

/// <summary>
/// LLM provider implementation using Azure OpenAI's chat completions API.
/// </summary>
public class AzureOpenAILlmProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly AzureOpenAILlmOptions _options;
    private readonly ILogger<AzureOpenAILlmProvider> _logger;

    private const string ApiVersion = "2024-02-01";

    public string ModelId => _options.DeploymentName;

    public AzureOpenAILlmProvider(
        HttpClient httpClient,
        IOptions<AzureOpenAILlmOptions> options,
        ILogger<AzureOpenAILlmProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_options.Endpoint.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Add("api-key", _options.ApiKey);
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

        _logger.LogDebug("Generating response with Azure OpenAI deployment {Deployment}", _options.DeploymentName);

        var request = new
        {
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = options.Temperature,
            max_tokens = options.MaxTokens
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"openai/deployments/{_options.DeploymentName}/chat/completions?api-version={ApiVersion}",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AzureOpenAIResponse>(cancellationToken: cancellationToken);

        if (result?.Choices == null || result.Choices.Length == 0)
        {
            throw new InvalidOperationException("Azure OpenAI returned no choices");
        }

        var text = result.Choices[0].Message.Content;

        _logger.LogDebug("Azure OpenAI generated {Length} characters", text?.Length ?? 0);

        return text ?? string.Empty;
    }

    private class AzureOpenAIResponse
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
