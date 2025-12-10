using DNFileRAG.Core.Models;

namespace DNFileRAG.Core.Interfaces;

/// <summary>
/// Interface for splitting text into overlapping chunks.
/// </summary>
public interface ITextChunker
{
    /// <summary>
    /// Splits text into overlapping chunks.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="chunkSize">Maximum size of each chunk in characters.</param>
    /// <param name="overlap">Number of characters to overlap between chunks.</param>
    /// <returns>List of text chunks.</returns>
    IReadOnlyList<TextChunk> Chunk(string text, int chunkSize, int overlap);

    /// <summary>
    /// Splits a parsed document into overlapping chunks, preserving page information.
    /// </summary>
    /// <param name="document">The parsed document to chunk.</param>
    /// <param name="chunkSize">Maximum size of each chunk in characters.</param>
    /// <param name="overlap">Number of characters to overlap between chunks.</param>
    /// <returns>List of text chunks with page information.</returns>
    IReadOnlyList<TextChunk> ChunkDocument(ParsedDocument document, int chunkSize, int overlap);
}
