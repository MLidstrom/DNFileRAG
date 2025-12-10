namespace DNFileRAG.Core.Models;

/// <summary>
/// Admin view of an indexed document.
/// </summary>
public class DocumentInfo
{
    /// <summary>
    /// SHA256 hash of the absolute file path.
    /// </summary>
    public required string FileId { get; init; }

    /// <summary>
    /// Full path to the document.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// File name (basename).
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// When the document was last indexed.
    /// </summary>
    public required DateTime LastIndexed { get; init; }

    /// <summary>
    /// Number of chunks stored for this document.
    /// </summary>
    public required int ChunkCount { get; init; }
}
