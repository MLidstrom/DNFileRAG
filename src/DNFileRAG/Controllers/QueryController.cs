using DNFileRAG.Core.Interfaces;
using DNFileRAG.Core.Models;
using DNFileRAG.Models;
using Microsoft.AspNetCore.Mvc;

namespace DNFileRAG.Controllers;

/// <summary>
/// Controller for RAG query operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QueryController : ControllerBase
{
    private readonly IRagEngine _ragEngine;
    private readonly ILogger<QueryController> _logger;

    public QueryController(IRagEngine ragEngine, ILogger<QueryController> logger)
    {
        _ragEngine = ragEngine;
        _logger = logger;
    }

    /// <summary>
    /// Process a RAG query and return an AI-generated answer with sources.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The query response with answer and sources.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(QueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QueryResponse>> Query(
        [FromBody] QueryRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received query: {Query}", request.Query);

        var ragQuery = new RagQuery
        {
            Query = request.Query,
            TopK = request.TopK,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            ConversationId = request.ConversationId,
            Filters = request.FilePaths != null
                ? new RagQueryFilters { FilePaths = request.FilePaths }
                : null
        };

        var result = await _ragEngine.QueryAsync(ragQuery, cancellationToken);

        var response = new QueryResponse
        {
            Answer = result.Answer,
            Sources = result.Sources.Select(s => new SourceInfo
            {
                FilePath = s.FilePath,
                FileName = s.FileName,
                ChunkIndex = s.ChunkIndex,
                PageNumber = s.PageNumber,
                Score = s.Score,
                Snippet = TruncateContent(s.Content, 200)
            }).ToList(),
            Metadata = new QueryMetadata
            {
                Model = result.Meta.Model,
                LatencyMs = result.Meta.LatencyMs,
                GuardrailsApplied = result.Meta.GuardrailsApplied,
                ConversationId = result.Meta.ConversationId
            }
        };

        _logger.LogInformation("Query completed in {LatencyMs}ms with {SourceCount} sources",
            response.Metadata.LatencyMs, response.Sources.Count);

        return Ok(response);
    }

    private static string? TruncateContent(string? content, int maxLength)
    {
        if (string.IsNullOrEmpty(content) || content.Length <= maxLength)
            return content;

        return content[..maxLength] + "...";
    }
}
