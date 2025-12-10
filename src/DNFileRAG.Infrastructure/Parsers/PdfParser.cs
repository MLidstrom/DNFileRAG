using DNFileRAG.Core.Interfaces;
using DNFileRAG.Core.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace DNFileRAG.Infrastructure.Parsers;

/// <summary>
/// Parser for PDF files using PdfPig.
/// </summary>
public class PdfParser : IDocumentParser
{
    private static readonly string[] Extensions = [".pdf"];

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

        // PdfPig operations are synchronous, wrap in Task for consistency
        var result = ParsePdf(filePath);
        return Task.FromResult(result);
    }

    private static ParsedDocument ParsePdf(string filePath)
    {
        using var document = PdfDocument.Open(filePath);

        var pages = new List<PageContent>();
        var allText = new System.Text.StringBuilder();

        foreach (var page in document.GetPages())
        {
            var pageText = ExtractPageText(page);

            pages.Add(new PageContent
            {
                PageNumber = page.Number,
                Content = pageText
            });

            if (allText.Length > 0)
                allText.AppendLine();

            allText.Append(pageText);
        }

        var metadata = ExtractMetadata(document);

        return new ParsedDocument
        {
            Content = allText.ToString(),
            Pages = pages,
            Metadata = new DocumentParseMetadata
            {
                Title = metadata.title,
                Author = metadata.author,
                CreatedDate = metadata.created,
                PageCount = document.NumberOfPages
            }
        };
    }

    private static string ExtractPageText(Page page)
    {
        var text = page.Text;

        // Normalize whitespace while preserving paragraph structure
        text = System.Text.RegularExpressions.Regex.Replace(text, @"[ \t]+", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\n{3,}", "\n\n");

        return text.Trim();
    }

    private static (string? title, string? author, DateTime? created) ExtractMetadata(PdfDocument document)
    {
        string? title = null;
        string? author = null;
        DateTime? created = null;

        var info = document.Information;

        if (!string.IsNullOrWhiteSpace(info.Title))
            title = info.Title;

        if (!string.IsNullOrWhiteSpace(info.Author))
            author = info.Author;

        // CreationDate is a string in PdfPig, try to parse it
        if (!string.IsNullOrWhiteSpace(info.CreationDate) &&
            DateTime.TryParse(info.CreationDate, out var parsedDate))
        {
            created = parsedDate;
        }

        return (title, author, created);
    }
}
