using DNFileRAG.Core.Interfaces;
using DNFileRAG.Models;
using Microsoft.AspNetCore.Mvc;

namespace DNFileRAG.Controllers;

/// <summary>
/// Controller for document management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IVectorStore _vectorStore;
    private readonly IIngestionPipeline _ingestionPipeline;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IVectorStore vectorStore,
        IIngestionPipeline ingestionPipeline,
        ILogger<DocumentsController> logger)
    {
        _vectorStore = vectorStore;
        _ingestionPipeline = ingestionPipeline;
        _logger = logger;
    }

    /// <summary>
    /// Get list of all indexed documents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of indexed documents.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(DocumentListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentListResponse>> GetDocuments(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting document list");

        var documents = await _vectorStore.GetDocumentListAsync(cancellationToken);

        var response = new DocumentListResponse
        {
            Documents = documents.Select(d => new DocumentInfoDto
            {
                FileId = d.FileId,
                FilePath = d.FilePath,
                FileName = d.FileName,
                LastIndexed = d.LastIndexed,
                ChunkCount = d.ChunkCount
            }).ToList(),
            TotalCount = documents.Count
        };

        _logger.LogDebug("Returning {Count} documents", response.TotalCount);
        return Ok(response);
    }

    /// <summary>
    /// Trigger reindexing of all documents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of documents reindexed.</returns>
    [HttpPost("reindex")]
    [ProducesResponseType(typeof(ReindexResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReindexResponse>> Reindex(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reindex requested");

        var count = await _ingestionPipeline.ReindexAllAsync(cancellationToken);

        _logger.LogInformation("Reindex completed, processed {Count} documents", count);

        return Ok(new ReindexResponse { DocumentsProcessed = count });
    }

    /// <summary>
    /// Delete a document from the index by file path.
    /// </summary>
    /// <param name="filePath">The file path to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of vectors deleted.</returns>
    [HttpDelete]
    [ProducesResponseType(typeof(DeleteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeleteResponse>> DeleteDocument(
        [FromQuery] string filePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return BadRequest("File path is required");
        }

        _logger.LogInformation("Delete requested for: {FilePath}", filePath);

        var deleted = await _ingestionPipeline.RemoveFileAsync(filePath, cancellationToken);

        _logger.LogInformation("Deleted {Count} vectors for: {FilePath}", deleted, filePath);

        return Ok(new DeleteResponse { VectorsDeleted = deleted });
    }
}

/// <summary>
/// Response for reindex operation.
/// </summary>
public class ReindexResponse
{
    /// <summary>
    /// Number of documents processed.
    /// </summary>
    public int DocumentsProcessed { get; init; }
}

/// <summary>
/// Response for delete operation.
/// </summary>
public class DeleteResponse
{
    /// <summary>
    /// Number of vectors deleted.
    /// </summary>
    public int VectorsDeleted { get; init; }
}
