namespace DNFileRAG.Core.Models;

/// <summary>
/// Response model for RAG queries.
/// </summary>
public class RagResponse
{
    /// <summary>
    /// The generated answer from the LLM.
    /// </summary>
    public required string Answer { get; init; }

    /// <summary>
    /// Source chunks that were used to generate the answer.
    /// </summary>
    public required IReadOnlyList<RagSource> Sources { get; init; }

    /// <summary>
    /// Metadata about the query processing.
    /// </summary>
    public required RagResponseMeta Meta { get; init; }
}

/// <summary>
/// Information about a source chunk used in the response.
/// </summary>
public class RagSource
{
    /// <summary>
    /// Path to the source document.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// File name of the source document.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Chunk index within the document.
    /// </summary>
    public required int ChunkIndex { get; init; }

    /// <summary>
    /// Page number if available.
    /// </summary>
    public int? PageNumber { get; init; }

    /// <summary>
    /// Similarity score (0-1).
    /// </summary>
    public required float Score { get; init; }

    /// <summary>
    /// The text content of this chunk.
    /// </summary>
    public string? Content { get; init; }
}

/// <summary>
/// Metadata about the RAG query processing.
/// </summary>
public class RagResponseMeta
{
    /// <summary>
    /// Whether guardrails were applied.
    /// </summary>
    public bool GuardrailsApplied { get; init; }

    /// <summary>
    /// The LLM model used.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// End-to-end processing latency in milliseconds.
    /// </summary>
    public required int LatencyMs { get; init; }

    /// <summary>
    /// Conversation ID if provided.
    /// </summary>
    public string? ConversationId { get; init; }
}
