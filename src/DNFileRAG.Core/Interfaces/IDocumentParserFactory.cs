namespace DNFileRAG.Core.Interfaces;

/// <summary>
/// Factory interface for selecting document parsers based on file extension.
/// </summary>
public interface IDocumentParserFactory
{
    /// <summary>
    /// Gets all supported file extensions across all parsers.
    /// </summary>
    IEnumerable<string> SupportedExtensions { get; }

    /// <summary>
    /// Gets a parser that can handle the given file extension.
    /// </summary>
    /// <param name="fileExtension">The file extension (including dot).</param>
    /// <returns>A parser that can handle the extension, or null if none found.</returns>
    IDocumentParser? GetParser(string fileExtension);

    /// <summary>
    /// Gets a parser for the given file path based on its extension.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>A parser that can handle the file, or null if none found.</returns>
    IDocumentParser? GetParserForFile(string filePath);

    /// <summary>
    /// Checks if any parser can handle the given file extension.
    /// </summary>
    bool CanParse(string fileExtension);

    /// <summary>
    /// Checks if any parser can handle the given file.
    /// </summary>
    bool CanParseFile(string filePath);
}
