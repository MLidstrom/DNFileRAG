using DNFileRAG.Core.Models;

namespace DNFileRAG.Core.Interfaces;

/// <summary>
/// Interface for vector database operations.
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Ensures the collection exists with the correct schema.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnsureCollectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts document chunks into the vector store.
    /// </summary>
    /// <param name="chunks">The chunks to upsert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpsertChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all chunks for a document by file ID.
    /// </summary>
    /// <param name="fileId">The file ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of vectors deleted.</returns>
    Task<int> DeleteByFileIdAsync(string fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for similar chunks using a query vector.
    /// </summary>
    /// <param name="queryVector">The query embedding vector.</param>
    /// <param name="topK">Number of results to return.</param>
    /// <param name="filters">Optional filters to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching chunks with scores.</returns>
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        float[] queryVector,
        int topK,
        SearchFilters? filters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of all indexed documents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of document info.</returns>
    Task<IReadOnlyList<DocumentInfo>> GetDocumentListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a document with the given file hash is already indexed.
    /// </summary>
    /// <param name="fileId">The file ID to check.</param>
    /// <param name="fileHash">The file hash to compare.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the document is already indexed with the same hash.</returns>
    Task<bool> IsDocumentIndexedAsync(string fileId, string fileHash, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a vector similarity search.
/// </summary>
public class SearchResult
{
    /// <summary>
    /// The chunk metadata.
    /// </summary>
    public required ChunkMetadata Metadata { get; init; }

    /// <summary>
    /// The chunk content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Similarity score (0-1).
    /// </summary>
    public required float Score { get; init; }
}

/// <summary>
/// Filters for vector search.
/// </summary>
public class SearchFilters
{
    /// <summary>
    /// Filter by file path prefixes.
    /// </summary>
    public string[]? FilePaths { get; init; }

    /// <summary>
    /// Only return active chunks.
    /// </summary>
    public bool? IsActive { get; init; }
}
