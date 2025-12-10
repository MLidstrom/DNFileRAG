using DNFileRAG.Core.Interfaces;
using DNFileRAG.Core.Models;

namespace DNFileRAG.Infrastructure.Services;

/// <summary>
/// Splits text into overlapping chunks for embedding.
/// Uses sentence-aware chunking to avoid breaking mid-sentence when possible.
/// </summary>
public class TextChunker : ITextChunker
{
    private static readonly char[] SentenceEnders = ['.', '!', '?', '\n'];
    private static readonly char[] WordBreaks = [' ', '\t', '\n', '\r'];

    /// <inheritdoc />
    public IReadOnlyList<TextChunk> Chunk(string text, int chunkSize, int overlap)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be positive.");
        if (overlap < 0)
            throw new ArgumentOutOfRangeException(nameof(overlap), "Overlap cannot be negative.");
        if (overlap >= chunkSize)
            throw new ArgumentException("Overlap must be less than chunk size.", nameof(overlap));

        if (string.IsNullOrWhiteSpace(text))
            return [];

        var chunks = new List<TextChunk>();
        var normalizedText = NormalizeText(text);

        int position = 0;
        int index = 0;

        while (position < normalizedText.Length)
        {
            int endPosition = Math.Min(position + chunkSize, normalizedText.Length);

            // Try to find a good break point (sentence end, then word break)
            if (endPosition < normalizedText.Length)
            {
                endPosition = FindBreakPoint(normalizedText, position, endPosition);
            }

            var chunkContent = normalizedText[position..endPosition].Trim();

            if (!string.IsNullOrWhiteSpace(chunkContent))
            {
                chunks.Add(new TextChunk
                {
                    Content = chunkContent,
                    Index = index++,
                    StartPosition = position,
                    EndPosition = endPosition
                });
            }

            // If we've reached the end of text, we're done
            if (endPosition >= normalizedText.Length)
                break;

            // Move position forward, accounting for overlap
            int advance = endPosition - position - overlap;
            if (advance <= 0)
                advance = Math.Max(1, endPosition - position); // Ensure we always advance

            position += advance;
        }

        return chunks;
    }

    /// <inheritdoc />
    public IReadOnlyList<TextChunk> ChunkDocument(ParsedDocument document, int chunkSize, int overlap)
    {
        ArgumentNullException.ThrowIfNull(document);

        // If document has page information, chunk page by page to preserve page numbers
        if (document.Pages is { Count: > 0 })
        {
            return ChunkWithPageInfo(document.Pages, chunkSize, overlap);
        }

        // Fall back to simple chunking without page info
        return Chunk(document.Content, chunkSize, overlap);
    }

    private IReadOnlyList<TextChunk> ChunkWithPageInfo(IReadOnlyList<PageContent> pages, int chunkSize, int overlap)
    {
        var chunks = new List<TextChunk>();
        int globalIndex = 0;
        int globalPosition = 0;

        foreach (var page in pages)
        {
            if (string.IsNullOrWhiteSpace(page.Content))
            {
                globalPosition += page.Content?.Length ?? 0;
                continue;
            }

            var normalizedContent = NormalizeText(page.Content);
            int position = 0;

            while (position < normalizedContent.Length)
            {
                int endPosition = Math.Min(position + chunkSize, normalizedContent.Length);

                if (endPosition < normalizedContent.Length)
                {
                    endPosition = FindBreakPoint(normalizedContent, position, endPosition);
                }

                var chunkContent = normalizedContent[position..endPosition].Trim();

                if (!string.IsNullOrWhiteSpace(chunkContent))
                {
                    chunks.Add(new TextChunk
                    {
                        Content = chunkContent,
                        Index = globalIndex++,
                        PageNumber = page.PageNumber,
                        StartPosition = globalPosition + position,
                        EndPosition = globalPosition + endPosition
                    });
                }

                // If we've reached the end of page content, move to next page
                if (endPosition >= normalizedContent.Length)
                    break;

                int advance = endPosition - position - overlap;
                if (advance <= 0)
                    advance = Math.Max(1, endPosition - position);

                position += advance;
            }

            globalPosition += page.Content.Length;
        }

        return chunks;
    }

    private static string NormalizeText(string text)
    {
        // Replace multiple whitespace with single space, but preserve paragraph breaks
        var normalized = System.Text.RegularExpressions.Regex.Replace(text, @"[ \t]+", " ");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\n{3,}", "\n\n");
        return normalized.Trim();
    }

    private static int FindBreakPoint(string text, int start, int idealEnd)
    {
        // First, try to find a sentence ending within reasonable distance
        int searchStart = Math.Max(start, idealEnd - 200); // Look back up to 200 chars

        for (int i = idealEnd - 1; i >= searchStart; i--)
        {
            if (Array.IndexOf(SentenceEnders, text[i]) >= 0)
            {
                // Make sure it's actually end of sentence (followed by space or end)
                if (i + 1 >= text.Length || char.IsWhiteSpace(text[i + 1]))
                {
                    return i + 1;
                }
            }
        }

        // If no sentence break, find word break
        for (int i = idealEnd - 1; i >= searchStart; i--)
        {
            if (Array.IndexOf(WordBreaks, text[i]) >= 0)
            {
                return i + 1;
            }
        }

        // No good break point found, use ideal end
        return idealEnd;
    }
}
