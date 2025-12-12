using DNFileRAG.Core.Configuration;
using DNFileRAG.Core.Interfaces;
using DNFileRAG.Infrastructure.Embeddings;
using DNFileRAG.Infrastructure.Llm;
using DNFileRAG.Infrastructure.Parsers;
using DNFileRAG.Infrastructure.Services;
using DNFileRAG.Infrastructure.VectorStore;

namespace DNFileRAG;

/// <summary>
/// Extension methods for configuring DNFileRAG services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all DNFileRAG services to the service collection.
    /// </summary>
    public static IServiceCollection AddDNFileRAGServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration sections
        services.Configure<EmbeddingOptions>(configuration.GetSection(EmbeddingOptions.SectionName));
        services.Configure<QdrantOptions>(configuration.GetSection(QdrantOptions.SectionName));
        services.Configure<LlmOptions>(configuration.GetSection(LlmOptions.SectionName));
        services.Configure<ChunkingOptions>(configuration.GetSection(ChunkingOptions.SectionName));
        services.Configure<FileWatcherOptions>(configuration.GetSection(FileWatcherOptions.SectionName));
        services.Configure<RagOptions>(configuration.GetSection(RagOptions.SectionName));
        services.Configure<ApiSecurityOptions>(configuration.GetSection(ApiSecurityOptions.SectionName));

        // Register core services
        services.AddSingleton<ITextChunker, TextChunker>();

        // Register document parsers as IDocumentParser implementations
        services.AddSingleton<IDocumentParser, PlainTextParser>();
        services.AddSingleton<IDocumentParser, HtmlParser>();
        services.AddSingleton<IDocumentParser, PdfParser>();
        services.AddSingleton<IDocumentParser, DocxParser>();
        services.AddSingleton<IDocumentParserFactory, DocumentParserFactory>();

        // Register embedding services using existing extension
        services.AddEmbeddingServices();

        // Register LLM services using existing extension
        services.AddLlmProviders();

        // Register vector store with HttpClient
        services.AddHttpClient<IVectorStore, QdrantVectorStore>();

        // Register pipeline services
        services.AddSingleton<IIngestionPipeline, IngestionPipelineService>();
        services.AddSingleton<IRagEngine, RagEngine>();

        // Register background services
        services.AddHostedService<FileWatcherService>();

        return services;
    }
}
