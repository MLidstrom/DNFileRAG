using DNFileRAG.Core.Models;

namespace DNFileRAG.Core.Interfaces;

/// <summary>
/// Interface for parsing documents of various formats.
/// </summary>
public interface IDocumentParser
{
    /// <summary>
    /// Gets the file extensions supported by this parser.
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>
    /// Checks if this parser can handle the given file extension.
    /// </summary>
    bool CanParse(string fileExtension);

    /// <summary>
    /// Parses a document file and extracts its text content.
    /// </summary>
    /// <param name="filePath">Path to the document file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parsed document with extracted text.</returns>
    Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken = default);
}
