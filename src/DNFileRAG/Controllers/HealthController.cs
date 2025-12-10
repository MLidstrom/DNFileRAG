using Microsoft.AspNetCore.Mvc;

namespace DNFileRAG.Controllers;

/// <summary>
/// Controller for health check endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint.
    /// </summary>
    /// <returns>Health status.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> GetHealth()
    {
        return Ok(new HealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Detailed health check with component status.
    /// </summary>
    /// <returns>Detailed health status.</returns>
    [HttpGet("detailed")]
    [ProducesResponseType(typeof(DetailedHealthResponse), StatusCodes.Status200OK)]
    public ActionResult<DetailedHealthResponse> GetDetailedHealth()
    {
        // In a real implementation, this would check:
        // - Vector store connectivity
        // - LLM provider availability
        // - Embedding provider availability
        // - File watcher status

        return Ok(new DetailedHealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Components = new Dictionary<string, ComponentHealth>
            {
                ["api"] = new() { Status = "Healthy" },
                ["vectorStore"] = new() { Status = "Healthy" },
                ["embeddingProvider"] = new() { Status = "Healthy" },
                ["llmProvider"] = new() { Status = "Healthy" },
                ["fileWatcher"] = new() { Status = "Healthy" }
            }
        });
    }
}

/// <summary>
/// Basic health response.
/// </summary>
public class HealthResponse
{
    /// <summary>
    /// Overall health status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Timestamp of the health check.
    /// </summary>
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Detailed health response with component statuses.
/// </summary>
public class DetailedHealthResponse : HealthResponse
{
    /// <summary>
    /// Individual component health statuses.
    /// </summary>
    public required Dictionary<string, ComponentHealth> Components { get; init; }
}

/// <summary>
/// Individual component health status.
/// </summary>
public class ComponentHealth
{
    /// <summary>
    /// Component status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Optional error message if unhealthy.
    /// </summary>
    public string? Error { get; init; }
}
