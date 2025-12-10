namespace DNFileRAG.Models;

/// <summary>
/// Information about an indexed document.
/// </summary>
public class DocumentInfoDto
{
    /// <summary>
    /// Unique file identifier.
    /// </summary>
    public required string FileId { get; init; }

    /// <summary>
    /// Full file path.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// File name.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// When the document was last indexed.
    /// </summary>
    public DateTime LastIndexed { get; init; }

    /// <summary>
    /// Number of chunks in the index.
    /// </summary>
    public int ChunkCount { get; init; }
}

/// <summary>
/// Response for document list endpoint.
/// </summary>
public class DocumentListResponse
{
    /// <summary>
    /// List of indexed documents.
    /// </summary>
    public required IReadOnlyList<DocumentInfoDto> Documents { get; init; }

    /// <summary>
    /// Total number of documents.
    /// </summary>
    public int TotalCount { get; init; }
}
