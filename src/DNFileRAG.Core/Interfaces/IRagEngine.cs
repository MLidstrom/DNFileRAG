using DNFileRAG.Core.Models;

namespace DNFileRAG.Core.Interfaces;

/// <summary>
/// Interface for the RAG query engine.
/// </summary>
public interface IRagEngine
{
    /// <summary>
    /// Processes a RAG query and generates a response.
    /// </summary>
    /// <param name="query">The RAG query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The RAG response with answer and sources.</returns>
    Task<RagResponse> QueryAsync(RagQuery query, CancellationToken cancellationToken = default);
}
