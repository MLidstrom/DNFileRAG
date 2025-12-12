using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
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

        if (searchResults.Count > 0)
        {
            _logger.LogDebug("Raw vector search scores: min={MinScore:F3}, max={MaxScore:F3}",
                searchResults.Min(r => r.Score),
                searchResults.Max(r => r.Score));
        }

        // Filter by minimum relevance score
        if (_options.MinRelevanceScore > 0)
        {
            var originalCount = searchResults.Count;
            searchResults = searchResults
                .Where(r => r.Score >= _options.MinRelevanceScore)
                .ToList();

            if (originalCount > searchResults.Count)
            {
                _logger.LogDebug("Filtered {Removed} results below minimum relevance score {MinScore}",
                    originalCount - searchResults.Count, _options.MinRelevanceScore);
            }
        }

        if (searchResults.Count == 0)
        {
            _logger.LogInformation("No relevant documents found for query (threshold: {MinScore})", _options.MinRelevanceScore);
            return CreateNoResultsResponse(query, stopwatch, guardrailsApplied);
        }

        _logger.LogDebug("Found {Count} relevant chunks (scores: {MinScore:F2} - {MaxScore:F2})",
            searchResults.Count, searchResults.Min(r => r.Score), searchResults.Max(r => r.Score));

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

        _logger.LogInformation("RAG query completed in {LatencyMs}ms with {ContextChunkCount} context chunks",
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
        // Basic sanitization - production systems should use content moderation APIs.
        // For this product/demo UX, we also avoid mentioning internal retrieval, documents, or sources.
        var text = answer.Trim();

        // Strip common citation formats
        text = Regex.Replace(text, @"\s*\[\s*source\s*\d+\s*\]\s*", " ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\(\s*source\s*\d+\s*\)", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bsource\s*\d+\b", "", RegexOptions.IgnoreCase);

        // Strip common "meta" lead-in sentences that mention context/documents/sources
        text = Regex.Replace(text,
            @"^\s*(based on|according to)\s+the\s+(provided\s+)?(context|information)[^.\n]*[.\n]+\s*",
            "",
            RegexOptions.IgnoreCase);
        text = Regex.Replace(text,
            @"^\s*(based on|according to)\s+the\s+(provided\s+)?(context|information)\s+from\s+[^.\n]*[.\n]+\s*",
            "",
            RegexOptions.IgnoreCase);

        // If the model still references documents/context, soften it.
        text = Regex.Replace(text, @"\bindexed\s+documents?\b", "our information", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bprovided\s+context\b", "available information", RegexOptions.IgnoreCase);

        // Clean up whitespace artifacts from removals
        text = Regex.Replace(text, @"[ \t]{2,}", " ");
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        return text.Trim();
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

        sb.AppendLine("Answer the user’s question using ONLY the information below.");
        sb.AppendLine("Do NOT mention sources, documents, context, snippets, citations, or retrieval.");
        sb.AppendLine("If the information is insufficient, say: \"I don't have that information.\"");
        sb.AppendLine();
        sb.AppendLine("=== INFORMATION ===");
        sb.AppendLine();

        for (var i = 0; i < searchResults.Count; i++)
        {
            var result = searchResults[i];
            sb.AppendLine($"Item {i + 1}:");
            sb.AppendLine(result.Content);
            sb.AppendLine();
        }

        sb.AppendLine("=== END INFORMATION ===");
        sb.AppendLine();
        sb.AppendLine("Question: " + query);
        sb.AppendLine();
        sb.AppendLine("Provide a direct, customer-friendly answer. Do not include citations.");

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
            Answer = "Sorry — I don't have that information. Could you share a bit more detail so I can help?",
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
