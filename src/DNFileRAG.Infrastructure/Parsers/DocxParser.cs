using DNFileRAG.Core.Interfaces;
using DNFileRAG.Core.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DNFileRAG.Infrastructure.Parsers;

/// <summary>
/// Parser for DOCX files using OpenXml.
/// </summary>
public class DocxParser : IDocumentParser
{
    private static readonly string[] Extensions = [".docx"];

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedExtensions => Extensions;

    /// <inheritdoc />
    public bool CanParse(string fileExtension)
    {
        ArgumentNullException.ThrowIfNull(fileExtension);
        return Extensions.Contains(fileExtension.ToLowerInvariant());
    }

    /// <inheritdoc />
    public Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found.", filePath);

        var result = ParseDocx(filePath);
        return Task.FromResult(result);
    }

    private static ParsedDocument ParseDocx(string filePath)
    {
        using var document = WordprocessingDocument.Open(filePath, false);

        var mainPart = document.MainDocumentPart;
        if (mainPart?.Document.Body == null)
        {
            return new ParsedDocument
            {
                Content = string.Empty,
                Metadata = new DocumentParseMetadata()
            };
        }

        var pages = ExtractPagesFromBody(mainPart.Document.Body);
        var allText = string.Join("\n\n", pages.Select(p => p.Content));
        var metadata = ExtractMetadata(document);

        return new ParsedDocument
        {
            Content = allText,
            Pages = pages.Count > 0 ? pages : null,
            Metadata = new DocumentParseMetadata
            {
                Title = metadata.title,
                Author = metadata.author,
                CreatedDate = metadata.created,
                PageCount = pages.Count > 0 ? pages.Count : null
            }
        };
    }

    private static List<PageContent> ExtractPagesFromBody(Body body)
    {
        var pages = new List<PageContent>();
        var currentPageText = new System.Text.StringBuilder();
        int currentPage = 1;

        foreach (var element in body.ChildElements)
        {
            // Check for page break
            if (HasPageBreak(element))
            {
                if (currentPageText.Length > 0)
                {
                    pages.Add(new PageContent
                    {
                        PageNumber = currentPage++,
                        Content = currentPageText.ToString().Trim()
                    });
                    currentPageText.Clear();
                }
            }

            var text = ExtractTextFromElement(element);
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (currentPageText.Length > 0)
                    currentPageText.AppendLine();
                currentPageText.Append(text);
            }
        }

        // Add remaining content as last page
        if (currentPageText.Length > 0)
        {
            pages.Add(new PageContent
            {
                PageNumber = currentPage,
                Content = currentPageText.ToString().Trim()
            });
        }

        return pages;
    }

    private static bool HasPageBreak(DocumentFormat.OpenXml.OpenXmlElement element)
    {
        if (element is Paragraph paragraph)
        {
            // Check for page break before paragraph
            var pPr = paragraph.GetFirstChild<ParagraphProperties>();
            if (pPr?.PageBreakBefore != null)
                return true;

            // Check for break elements
            var runs = paragraph.Descendants<Run>();
            foreach (var run in runs)
            {
                var breaks = run.Descendants<Break>();
                if (breaks.Any(b => b.Type?.Value == BreakValues.Page))
                    return true;
            }
        }

        return false;
    }

    private static string ExtractTextFromElement(DocumentFormat.OpenXml.OpenXmlElement element)
    {
        if (element is Paragraph paragraph)
        {
            var text = paragraph.InnerText;
            return string.IsNullOrWhiteSpace(text) ? string.Empty : text;
        }

        if (element is Table table)
        {
            var tableText = new System.Text.StringBuilder();
            foreach (var row in table.Descendants<TableRow>())
            {
                var cells = row.Descendants<TableCell>()
                    .Select(c => c.InnerText?.Trim() ?? string.Empty);
                tableText.AppendLine(string.Join(" | ", cells));
            }
            return tableText.ToString().Trim();
        }

        return string.Empty;
    }

    private static (string? title, string? author, DateTime? created) ExtractMetadata(WordprocessingDocument document)
    {
        string? title = null;
        string? author = null;
        DateTime? created = null;

        var props = document.PackageProperties;
        if (props != null)
        {
            if (!string.IsNullOrWhiteSpace(props.Title))
                title = props.Title;

            if (!string.IsNullOrWhiteSpace(props.Creator))
                author = props.Creator;

            if (props.Created.HasValue)
                created = props.Created.Value;
        }

        return (title, author, created);
    }
}
