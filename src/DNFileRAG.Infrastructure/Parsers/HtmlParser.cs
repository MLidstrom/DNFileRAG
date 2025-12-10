using DNFileRAG.Core.Interfaces;
using DNFileRAG.Core.Models;
using HtmlAgilityPack;

namespace DNFileRAG.Infrastructure.Parsers;

/// <summary>
/// Parser for HTML files using HtmlAgilityPack.
/// </summary>
public class HtmlParser : IDocumentParser
{
    private static readonly string[] Extensions = [".html", ".htm"];
    private static readonly HashSet<string> ScriptStyleTags = ["script", "style", "noscript"];

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

        var html = await File.ReadAllTextAsync(filePath, cancellationToken);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove script and style elements
        RemoveNodes(doc, ScriptStyleTags);

        // Extract text content
        var content = ExtractText(doc.DocumentNode);
        var title = ExtractTitle(doc);

        var fileInfo = new FileInfo(filePath);

        return new ParsedDocument
        {
            Content = content,
            Metadata = new DocumentParseMetadata
            {
                Title = title,
                CreatedDate = fileInfo.CreationTimeUtc
            }
        };
    }

    private static void RemoveNodes(HtmlDocument doc, HashSet<string> tagNames)
    {
        var nodesToRemove = doc.DocumentNode
            .Descendants()
            .Where(n => tagNames.Contains(n.Name.ToLowerInvariant()))
            .ToList();

        foreach (var node in nodesToRemove)
        {
            node.Remove();
        }
    }

    private static string ExtractText(HtmlNode node)
    {
        var text = HtmlEntity.DeEntitize(node.InnerText ?? string.Empty);

        // Normalize whitespace
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }

    private static string? ExtractTitle(HtmlDocument doc)
    {
        var titleNode = doc.DocumentNode.SelectSingleNode("//title");
        if (titleNode?.InnerText != null)
        {
            var text = titleNode.InnerText.Trim();
            return string.IsNullOrEmpty(text) ? null : HtmlEntity.DeEntitize(text);
        }

        // Try meta og:title
        var ogTitle = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
        var ogContent = ogTitle?.GetAttributeValue("content", string.Empty);
        if (!string.IsNullOrEmpty(ogContent))
        {
            return ogContent;
        }

        // Try h1
        var h1 = doc.DocumentNode.SelectSingleNode("//h1");
        if (h1?.InnerText != null)
        {
            var text = h1.InnerText.Trim();
            return string.IsNullOrEmpty(text) ? null : HtmlEntity.DeEntitize(text);
        }

        return null;
    }
}
