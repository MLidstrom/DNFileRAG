using DNFileRAG.Core.Configuration;
using DNFileRAG.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DNFileRAG.Infrastructure.Llm;

/// <summary>
/// Factory for creating LLM providers based on configuration.
/// </summary>
public class LlmProviderFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<LlmOptions> _options;
    private readonly ILoggerFactory _loggerFactory;

    public LlmProviderFactory(
        IHttpClientFactory httpClientFactory,
        IOptions<LlmOptions> options,
        ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Creates the configured LLM provider.
    /// </summary>
    public ILlmProvider CreateProvider()
    {
        var provider = _options.Value.Provider.ToLowerInvariant();
        var httpClient = _httpClientFactory.CreateClient($"llm-{provider}");
        var opts = _options.Value;

        return provider switch
        {
            "openai" => new OpenAILlmProvider(
                httpClient,
                Options.Create(new OpenAILlmOptions { ApiKey = opts.OpenAI.ApiKey, Model = opts.OpenAI.Model }),
                _loggerFactory.CreateLogger<OpenAILlmProvider>()),
            "azureopenai" => new AzureOpenAILlmProvider(
                httpClient,
                Options.Create(new AzureOpenAILlmOptions
                {
                    Endpoint = opts.AzureOpenAI.Endpoint,
                    ApiKey = opts.AzureOpenAI.ApiKey,
                    DeploymentName = opts.AzureOpenAI.DeploymentName
                }),
                _loggerFactory.CreateLogger<AzureOpenAILlmProvider>()),
            "anthropic" => new AnthropicLlmProvider(
                httpClient,
                Options.Create(new AnthropicLlmOptions { ApiKey = opts.Anthropic.ApiKey, Model = opts.Anthropic.Model }),
                _loggerFactory.CreateLogger<AnthropicLlmProvider>()),
            "ollama" => new OllamaLlmProvider(
                httpClient,
                Options.Create(new OllamaLlmOptions { BaseUrl = opts.Ollama.BaseUrl, Model = opts.Ollama.Model }),
                _loggerFactory.CreateLogger<OllamaLlmProvider>()),
            _ => throw new NotSupportedException($"LLM provider '{_options.Value.Provider}' is not supported. " +
                "Supported providers: OpenAI, AzureOpenAI, Anthropic, Ollama")
        };
    }
}

/// <summary>
/// Extension methods for registering LLM providers with DI.
/// </summary>
public static class LlmProviderServiceCollectionExtensions
{
    /// <summary>
    /// Adds LLM provider services to the service collection.
    /// </summary>
    public static IServiceCollection AddLlmProviders(this IServiceCollection services)
    {
        // Register named HTTP clients for each provider (won't instantiate providers)
        services.AddHttpClient("llm-openai");
        services.AddHttpClient("llm-azureopenai");
        services.AddHttpClient("llm-anthropic");
        services.AddHttpClient("llm-ollama");

        // Register factory
        services.AddSingleton<LlmProviderFactory>();

        // Register ILlmProvider using factory (lazy creation)
        services.AddSingleton<ILlmProvider>(sp => sp.GetRequiredService<LlmProviderFactory>().CreateProvider());

        return services;
    }
}
