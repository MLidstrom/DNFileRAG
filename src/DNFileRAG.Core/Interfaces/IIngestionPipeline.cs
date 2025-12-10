namespace DNFileRAG.Core.Interfaces;

/// <summary>
/// Interface for the document ingestion pipeline.
/// </summary>
public interface IIngestionPipeline
{
    /// <summary>
    /// Processes a new or updated file.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the file was processed, false if skipped (unchanged).</returns>
    Task<bool> ProcessFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a file from the index.
    /// </summary>
    /// <param name="filePath">Path to the file that was deleted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of vectors removed.</returns>
    Task<int> RemoveFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a full reindex of all documents in the watched directory.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of documents processed.</returns>
    Task<int> ReindexAllAsync(CancellationToken cancellationToken = default);
}
