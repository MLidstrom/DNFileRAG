using DNFileRAG.Core.Configuration;
using DNFileRAG.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DNFileRAG.Infrastructure.Embeddings;

/// <summary>
/// Factory for creating the configured embedding provider.
/// </summary>
public class EmbeddingProviderFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<EmbeddingOptions> _options;
    private readonly ILoggerFactory _loggerFactory;

    public EmbeddingProviderFactory(
        IHttpClientFactory httpClientFactory,
        IOptions<EmbeddingOptions> options,
        ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Creates the embedding provider based on configuration.
    /// </summary>
    public IEmbeddingProvider CreateProvider()
    {
        var provider = _options.Value.Provider.ToLowerInvariant();
        var httpClient = _httpClientFactory.CreateClient(provider);

        return provider switch
        {
            "openai" => new OpenAIEmbeddingProvider(
                httpClient,
                _options,
                _loggerFactory.CreateLogger<OpenAIEmbeddingProvider>()),
            "azureopenai" or "azure" => new AzureOpenAIEmbeddingProvider(
                httpClient,
                _options,
                _loggerFactory.CreateLogger<AzureOpenAIEmbeddingProvider>()),
            "ollama" => new OllamaEmbeddingProvider(
                httpClient,
                _options,
                _loggerFactory.CreateLogger<OllamaEmbeddingProvider>()),
            _ => throw new InvalidOperationException($"Unknown embedding provider: {_options.Value.Provider}")
        };
    }
}

/// <summary>
/// Extension methods for registering embedding services.
/// </summary>
public static class EmbeddingServiceExtensions
{
    /// <summary>
    /// Adds embedding services to the service collection.
    /// </summary>
    public static IServiceCollection AddEmbeddingServices(this IServiceCollection services)
    {
        // Register named HTTP clients for each provider (won't instantiate providers)
        services.AddHttpClient("openai");
        services.AddHttpClient("azureopenai");
        services.AddHttpClient("azure");
        services.AddHttpClient("ollama");

        // Register factory
        services.AddSingleton<EmbeddingProviderFactory>();

        // Register IEmbeddingProvider using factory (lazy creation)
        services.AddSingleton<IEmbeddingProvider>(sp =>
        {
            var factory = sp.GetRequiredService<EmbeddingProviderFactory>();
            return factory.CreateProvider();
        });

        return services;
    }
}
