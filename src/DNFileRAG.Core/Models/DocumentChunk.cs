namespace DNFileRAG.Core.Models;

/// <summary>
/// Represents a chunk of text from a document with its embedding.
/// </summary>
public class DocumentChunk
{
    /// <summary>
    /// Unique identifier for this chunk (typically file_id + chunk_index).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The text content of the chunk.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The embedding vector for this chunk.
    /// </summary>
    public required float[] Embedding { get; init; }

    /// <summary>
    /// Metadata associated with this chunk.
    /// </summary>
    public required ChunkMetadata Metadata { get; init; }
}

/// <summary>
/// Metadata stored with each document chunk in the vector database.
/// </summary>
public class ChunkMetadata
{
    /// <summary>
    /// SHA256 hash of the absolute file path.
    /// </summary>
    public required string FileId { get; init; }

    /// <summary>
    /// Full path to the source document.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// File name (basename).
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// SHA256 hash of the file contents.
    /// </summary>
    public required string FileHash { get; init; }

    /// <summary>
    /// Index of this chunk within the document.
    /// </summary>
    public required int ChunkIndex { get; init; }

    /// <summary>
    /// Page number if available (for PDF and paginated formats).
    /// </summary>
    public int? PageNumber { get; init; }

    /// <summary>
    /// When the chunk was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the chunk was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this chunk is active (for soft-delete support).
    /// </summary>
    public bool IsActive { get; init; } = true;
}
