using DNFileRAG.Core.Interfaces;
using DNFileRAG.Core.Models;

namespace DNFileRAG.Infrastructure.Parsers;

/// <summary>
/// Parser for plain text and markdown files (.txt, .md).
/// </summary>
public class PlainTextParser : IDocumentParser
{
    private static readonly string[] Extensions = [".txt", ".md"];

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedExtensions => Extensions;

    /// <inheritdoc />
    public bool CanParse(string fileExtension)
    {
        ArgumentNullException.ThrowIfNull(fileExtension);
        return Extensions.Contains(fileExtension.ToLowerInvariant());
    }

    /// <inheritdoc />
    public async Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found.", filePath);

        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        var fileInfo = new FileInfo(filePath);

        return new ParsedDocument
        {
            Content = content,
            Metadata = new DocumentParseMetadata
            {
                CreatedDate = fileInfo.CreationTimeUtc
            }
        };
    }
}
