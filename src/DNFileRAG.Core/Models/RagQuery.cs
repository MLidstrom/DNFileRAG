namespace DNFileRAG.Core.Models;

/// <summary>
/// Request model for RAG queries.
/// </summary>
public class RagQuery
{
    /// <summary>
    /// The user's query text.
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// Number of chunks to retrieve.
    /// </summary>
    public int? TopK { get; init; }

    /// <summary>
    /// Sampling temperature for LLM generation.
    /// </summary>
    public float? Temperature { get; init; }

    /// <summary>
    /// Maximum tokens for LLM output.
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Optional filters to apply to the search.
    /// </summary>
    public RagQueryFilters? Filters { get; init; }

    /// <summary>
    /// Optional conversation ID to correlate queries in a thread.
    /// </summary>
    public string? ConversationId { get; init; }
}

/// <summary>
/// Filters for RAG queries.
/// </summary>
public class RagQueryFilters
{
    /// <summary>
    /// Restrict search to documents with these file paths (prefix match).
    /// </summary>
    public string[]? FilePaths { get; init; }

    /// <summary>
    /// Restrict search to documents with these tags.
    /// </summary>
    public string[]? Tags { get; init; }
}
