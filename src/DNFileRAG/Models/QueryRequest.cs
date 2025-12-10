using System.ComponentModel.DataAnnotations;

namespace DNFileRAG.Models;

/// <summary>
/// Request model for RAG query endpoint.
/// </summary>
public class QueryRequest
{
    /// <summary>
    /// The user's question.
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(4000)]
    public required string Query { get; init; }

    /// <summary>
    /// Number of document chunks to retrieve for context.
    /// </summary>
    [Range(1, 20)]
    public int? TopK { get; init; }

    /// <summary>
    /// LLM sampling temperature (0.0 to 1.0).
    /// </summary>
    [Range(0.0, 1.0)]
    public float? Temperature { get; init; }

    /// <summary>
    /// Maximum tokens for LLM response.
    /// </summary>
    [Range(1, 4096)]
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Optional file path filters.
    /// </summary>
    public string[]? FilePaths { get; init; }

    /// <summary>
    /// Optional conversation ID for tracking.
    /// </summary>
    public string? ConversationId { get; init; }
}
