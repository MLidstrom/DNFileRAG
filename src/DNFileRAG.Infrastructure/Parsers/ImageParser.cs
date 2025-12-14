using DNFileRAG.Core.Interfaces;
using DNFileRAG.Core.Models;
using Microsoft.Extensions.Logging;

namespace DNFileRAG.Infrastructure.Parsers;

/// <summary>
/// Parser for common image formats (PNG/JPG/WEBP). Uses IVisionTextExtractor to extract text and description.
/// </summary>
public sealed class ImageParser : IDocumentParser
{
    private static readonly string[] Extensions = [".png", ".jpg", ".jpeg", ".webp"];

    private readonly IVisionTextExtractor _vision;
    private readonly ILogger<ImageParser> _logger;

    public ImageParser(IVisionTextExtractor vision, ILogger<ImageParser> logger)
    {
        _vision = vision;
        _logger = logger;
    }

    public IReadOnlyList<string> SupportedExtensions => Extensions;

    public bool CanParse(string fileExtension)
    {
        ArgumentNullException.ThrowIfNull(fileExtension);
        return Extensions.Contains(fileExtension.ToLowerInvariant());
    }

    public async Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found.", filePath);

        var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var fileName = Path.GetFileName(filePath);

        _logger.LogDebug("Parsing image: {FilePath}", filePath);
        var extracted = await _vision.ExtractAsync(bytes, fileName, cancellationToken);

        // Build a single text blob for indexing.
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Image: {fileName}");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(extracted.ExtractedText))
        {
            sb.AppendLine("Extracted text:");
            sb.AppendLine(extracted.ExtractedText.Trim());
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(extracted.Description))
        {
            sb.AppendLine("Description:");
            sb.AppendLine(extracted.Description.Trim());
        }

        var fileInfo = new FileInfo(filePath);
        return new ParsedDocument
        {
            Content = sb.ToString().Trim(),
            Metadata = new DocumentParseMetadata
            {
                CreatedDate = fileInfo.CreationTimeUtc,
                Title = fileName
            }
        };
    }
}


