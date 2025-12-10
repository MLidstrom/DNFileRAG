namespace DNFileRAG.Models;

/// <summary>
/// Response model for RAG query endpoint.
/// </summary>
public class QueryResponse
{
    /// <summary>
    /// The generated answer.
    /// </summary>
    public required string Answer { get; init; }

    /// <summary>
    /// Sources used to generate the answer.
    /// </summary>
    public required IReadOnlyList<SourceInfo> Sources { get; init; }

    /// <summary>
    /// Processing metadata.
    /// </summary>
    public required QueryMetadata Metadata { get; init; }
}

/// <summary>
/// Information about a source document chunk.
/// </summary>
public class SourceInfo
{
    /// <summary>
    /// Path to the source document.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// File name.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Chunk index within the document.
    /// </summary>
    public int ChunkIndex { get; init; }

    /// <summary>
    /// Page number if available.
    /// </summary>
    public int? PageNumber { get; init; }

    /// <summary>
    /// Relevance score (0-1).
    /// </summary>
    public float Score { get; init; }

    /// <summary>
    /// Snippet of the content.
    /// </summary>
    public string? Snippet { get; init; }
}

/// <summary>
/// Metadata about query processing.
/// </summary>
public class QueryMetadata
{
    /// <summary>
    /// LLM model used.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// Processing latency in milliseconds.
    /// </summary>
    public int LatencyMs { get; init; }

    /// <summary>
    /// Whether guardrails were applied.
    /// </summary>
    public bool GuardrailsApplied { get; init; }

    /// <summary>
    /// Conversation ID if provided.
    /// </summary>
    public string? ConversationId { get; init; }
}
