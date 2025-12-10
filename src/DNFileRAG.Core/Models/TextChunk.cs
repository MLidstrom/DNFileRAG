namespace DNFileRAG.Core.Models;

/// <summary>
/// A chunk of text ready for embedding.
/// </summary>
public class TextChunk
{
    /// <summary>
    /// The text content of the chunk.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Index of this chunk within the document.
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Page number if available (for paginated formats).
    /// </summary>
    public int? PageNumber { get; init; }

    /// <summary>
    /// Starting character position in the original document.
    /// </summary>
    public int StartPosition { get; init; }

    /// <summary>
    /// Ending character position in the original document.
    /// </summary>
    public int EndPosition { get; init; }
}
