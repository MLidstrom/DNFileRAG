namespace DNFileRAG.Core.Models;

/// <summary>
/// Result of parsing a document file.
/// </summary>
public class ParsedDocument
{
    /// <summary>
    /// The extracted text content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Page-wise content for paginated formats (PDF, DOCX).
    /// </summary>
    public IReadOnlyList<PageContent>? Pages { get; init; }

    /// <summary>
    /// Optional metadata extracted from the document.
    /// </summary>
    public DocumentParseMetadata? Metadata { get; init; }
}

/// <summary>
/// Content from a single page.
/// </summary>
public class PageContent
{
    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public required int PageNumber { get; init; }

    /// <summary>
    /// Text content of the page.
    /// </summary>
    public required string Content { get; init; }
}

/// <summary>
/// Optional metadata extracted during document parsing.
/// </summary>
public class DocumentParseMetadata
{
    /// <summary>
    /// Document title if available.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Document author if available.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Creation date if available.
    /// </summary>
    public DateTime? CreatedDate { get; init; }

    /// <summary>
    /// Total page count for paginated formats.
    /// </summary>
    public int? PageCount { get; init; }
}
