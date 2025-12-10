using System.Diagnostics;
using System.Text;
using DNFileRAG.Core.Configuration;
using DNFileRAG.Core.Interfaces;
using DNFileRAG.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DNFileRAG.Infrastructure.Services;

/// <summary>
/// RAG (Retrieval-Augmented Generation) engine that processes queries
/// by retrieving relevant context and generating LLM responses.
/// </summary>
public class RagEngine : IRagEngine
{
    private readonly RagOptions _options;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IVectorStore _vectorStore;
    private readonly ILlmProvider _llmProvider;
    private readonly ILogger<RagEngine> _logger;

    public RagEngine(
        IOptions<RagOptions> options,
        IEmbeddingProvider embeddingProvider,
        IVectorStore vectorStore,
        ILlmProvider llmProvider,
        ILogger<RagEngine> logger)
    {
        _options = options.Value;
        _embeddingProvider = embeddingProvider;
        _vectorStore = vectorStore;
        _llmProvider = llmProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RagResponse> QueryAsync(RagQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Query);

        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Processing RAG query: {Query}", query.Query);

        // Apply input guardrails
        var sanitizedQuery = ApplyInputGuardrails(query.Query);
        var guardrailsApplied = sanitizedQuery != query.Query;

        // Get query parameters with defaults
        var topK = query.TopK ?? _options.DefaultTopK;
        var temperature = query.Temperature ?? _options.DefaultTemperature;
        var maxTokens = query.MaxTokens ?? _options.DefaultMaxTokens;

        // Generate query embedding
        _logger.LogDebug("Generating query embedding");
        var queryEmbedding = await _embeddingProvider.GenerateEmbeddingAsync(sanitizedQuery, cancellationToken);

        // Build search filters
        var searchFilters = BuildSearchFilters(query.Filters);

        // Search vector store
        _logger.LogDebug("Searching vector store for top {TopK} results", topK);
        var searchResults = await _vectorStore.SearchAsync(queryEmbedding, topK, searchFilters, cancellationToken);

        if (searchResults.Count == 0)
        {
            _logger.LogInformation("No relevant documents found for query");
            return CreateNoResultsResponse(query, stopwatch, guardrailsApplied);
        }

        _logger.LogDebug("Found {Count} relevant chunks", searchResults.Count);

        // Build prompt with context
        var systemPrompt = _options.SystemPrompt;
        var userPrompt = BuildUserPrompt(sanitizedQuery, searchResults);

        // Generate LLM response
        _logger.LogDebug("Generating LLM response with model {Model}", _llmProvider.ModelId);
        var llmOptions = new LlmGenerationOptions
        {
            Temperature = temperature,
            MaxTokens = maxTokens
        };

        var answer = await _llmProvider.GenerateAsync(systemPrompt, userPrompt, llmOptions, cancellationToken);

        // Apply output guardrails
        var sanitizedAnswer = ApplyOutputGuardrails(answer);
        if (sanitizedAnswer != answer)
        {
            guardrailsApplied = true;
        }

        stopwatch.Stop();

        // Build response
        var response = new RagResponse
        {
            Answer = sanitizedAnswer,
            Sources = BuildSources(searchResults),
            Meta = new RagResponseMeta
            {
                GuardrailsApplied = guardrailsApplied,
                Model = _llmProvider.ModelId,
                LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                ConversationId = query.ConversationId
            }
        };

        _logger.LogInformation("RAG query completed in {LatencyMs}ms with {SourceCount} sources",
            response.Meta.LatencyMs, response.Sources.Count);

        return response;
    }

    /// <summary>
    /// Applies input guardrails to sanitize the query.
    /// </summary>
    private static string ApplyInputGuardrails(string query)
    {
        // Basic length limit
        const int maxQueryLength = 4000;
        if (query.Length > maxQueryLength)
        {
            query = query[..maxQueryLength];
        }

        // Remove potential prompt injection patterns
        // This is a basic implementation - production systems should use more sophisticated detection
        var sanitized = query
            .Replace("ignore previous instructions", "", StringComparison.OrdinalIgnoreCase)
            .Replace("disregard all prior", "", StringComparison.OrdinalIgnoreCase)
            .Replace("system:", "", StringComparison.OrdinalIgnoreCase);

        return sanitized.Trim();
    }

    /// <summary>
    /// Applies output guardrails to sanitize the LLM response.
    /// </summary>
    private static string ApplyOutputGuardrails(string answer)
    {
        // Basic sanitization - production systems should use content moderation APIs
        return answer.Trim();
    }

    /// <summary>
    /// Builds search filters from query filters.
    /// </summary>
    private static SearchFilters? BuildSearchFilters(RagQueryFilters? queryFilters)
    {
        if (queryFilters == null)
        {
            return new SearchFilters { IsActive = true };
        }

        return new SearchFilters
        {
            FilePaths = queryFilters.FilePaths,
            IsActive = true
        };
    }

    /// <summary>
    /// Builds the user prompt with retrieved context.
    /// </summary>
    private static string BuildUserPrompt(string query, IReadOnlyList<SearchResult> searchResults)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Based on the following context from documents, please answer the question.");
        sb.AppendLine();
        sb.AppendLine("=== CONTEXT ===");
        sb.AppendLine();

        for (var i = 0; i < searchResults.Count; i++)
        {
            var result = searchResults[i];
            sb.AppendLine($"[Source {i + 1}: {result.Metadata.FileName}");
            if (result.Metadata.PageNumber.HasValue)
            {
                sb.Append($", Page {result.Metadata.PageNumber}");
            }
            sb.AppendLine($"]");
            sb.AppendLine(result.Content);
            sb.AppendLine();
        }

        sb.AppendLine("=== END CONTEXT ===");
        sb.AppendLine();
        sb.AppendLine("Question: " + query);
        sb.AppendLine();
        sb.AppendLine("Please provide a comprehensive answer based on the context above. If the context doesn't contain relevant information, say so. Cite the source numbers (e.g., [Source 1]) when referencing specific information.");

        return sb.ToString();
    }

    /// <summary>
    /// Builds the source list from search results.
    /// </summary>
    private static IReadOnlyList<RagSource> BuildSources(IReadOnlyList<SearchResult> searchResults)
    {
        return searchResults.Select(r => new RagSource
        {
            FilePath = r.Metadata.FilePath,
            FileName = r.Metadata.FileName,
            ChunkIndex = r.Metadata.ChunkIndex,
            PageNumber = r.Metadata.PageNumber,
            Score = r.Score,
            Content = r.Content
        }).ToList();
    }

    /// <summary>
    /// Creates a response when no relevant documents are found.
    /// </summary>
    private RagResponse CreateNoResultsResponse(RagQuery query, Stopwatch stopwatch, bool guardrailsApplied)
    {
        stopwatch.Stop();
        return new RagResponse
        {
            Answer = "I couldn't find any relevant information in the indexed documents to answer your question. Please try rephrasing your question or ensure the relevant documents have been indexed.",
            Sources = [],
            Meta = new RagResponseMeta
            {
                GuardrailsApplied = guardrailsApplied,
                Model = _llmProvider.ModelId,
                LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                ConversationId = query.ConversationId
            }
        };
    }
}
