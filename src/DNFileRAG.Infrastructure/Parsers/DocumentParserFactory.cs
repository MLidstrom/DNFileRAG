using DNFileRAG.Core.Interfaces;

namespace DNFileRAG.Infrastructure.Parsers;

/// <summary>
/// Factory for selecting the appropriate document parser based on file extension.
/// </summary>
public class DocumentParserFactory : IDocumentParserFactory
{
    private readonly IReadOnlyList<IDocumentParser> _parsers;

    public DocumentParserFactory(IEnumerable<IDocumentParser>? parsers = null)
    {
        _parsers = parsers?.ToList() ?? CreateDefaultParsers();
    }

    /// <summary>
    /// Gets all supported file extensions across all parsers.
    /// </summary>
    public IEnumerable<string> SupportedExtensions =>
        _parsers.SelectMany(p => p.SupportedExtensions).Distinct();

    /// <summary>
    /// Gets a parser that can handle the given file extension.
    /// </summary>
    /// <param name="fileExtension">The file extension (including dot).</param>
    /// <returns>A parser that can handle the extension, or null if none found.</returns>
    public IDocumentParser? GetParser(string fileExtension)
    {
        ArgumentNullException.ThrowIfNull(fileExtension);
        return _parsers.FirstOrDefault(p => p.CanParse(fileExtension));
    }

    /// <summary>
    /// Gets a parser for the given file path based on its extension.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>A parser that can handle the file, or null if none found.</returns>
    public IDocumentParser? GetParserForFile(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        var extension = Path.GetExtension(filePath);
        return string.IsNullOrEmpty(extension) ? null : GetParser(extension);
    }

    /// <summary>
    /// Checks if any parser can handle the given file extension.
    /// </summary>
    public bool CanParse(string fileExtension)
    {
        ArgumentNullException.ThrowIfNull(fileExtension);
        return _parsers.Any(p => p.CanParse(fileExtension));
    }

    /// <summary>
    /// Checks if any parser can handle the given file.
    /// </summary>
    public bool CanParseFile(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        var extension = Path.GetExtension(filePath);
        return !string.IsNullOrEmpty(extension) && CanParse(extension);
    }

    private static List<IDocumentParser> CreateDefaultParsers() =>
    [
        new PlainTextParser(),
        new HtmlParser(),
        new PdfParser(),
        new DocxParser()
    ];
}
