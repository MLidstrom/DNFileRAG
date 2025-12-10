namespace DNFileRAG.Core.Models;

/// <summary>
/// Represents an indexed document in the system.
/// </summary>
public class Document
{
    /// <summary>
    /// SHA256 hash of the absolute file path - deterministic identifier.
    /// </summary>
    public required string FileId { get; init; }

    /// <summary>
    /// Full path to the document file.
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
    /// When the document was first indexed.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the document was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Number of chunks created from this document.
    /// </summary>
    public int ChunkCount { get; set; }
}
