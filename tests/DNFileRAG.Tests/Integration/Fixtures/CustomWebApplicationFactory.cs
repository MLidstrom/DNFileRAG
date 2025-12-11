using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DNFileRAG.Core.Interfaces;
using DNFileRAG.Core.Models;
using Moq;

namespace DNFileRAG.Tests.Integration.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for integration testing that replaces
/// external dependencies with mocks.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IVectorStore> MockVectorStore { get; } = new();
    public Mock<IEmbeddingProvider> MockEmbeddingProvider { get; } = new();
    public Mock<ILlmProvider> MockLlmProvider { get; } = new();
    public Mock<IRagEngine> MockRagEngine { get; } = new();
    public Mock<IIngestionPipeline> MockIngestionPipeline { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the real implementations
            services.RemoveAll<IVectorStore>();
            services.RemoveAll<IEmbeddingProvider>();
            services.RemoveAll<ILlmProvider>();
            services.RemoveAll<IRagEngine>();
            services.RemoveAll<IIngestionPipeline>();

            // Remove background services to prevent them from running during tests
            var descriptorsToRemove = services
                .Where(d => d.ServiceType.Name.Contains("FileWatcher") ||
                            d.ServiceType.Name.Contains("HostedService"))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Register mocks
            services.AddSingleton(MockVectorStore.Object);
            services.AddSingleton(MockEmbeddingProvider.Object);
            services.AddSingleton(MockLlmProvider.Object);
            services.AddSingleton(MockRagEngine.Object);
            services.AddSingleton(MockIngestionPipeline.Object);
        });
    }

    /// <summary>
    /// Sets up default mock behaviors for common test scenarios.
    /// </summary>
    public void SetupDefaultMocks()
    {
        // Default embedding provider behavior
        MockEmbeddingProvider
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[1024]); // Default vector size

        // Default vector store behavior
        MockVectorStore
            .Setup(x => x.GetDocumentListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentInfo>());

        // Default RAG engine behavior
        MockRagEngine
            .Setup(x => x.QueryAsync(It.IsAny<RagQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagResponse
            {
                Answer = "This is a test answer.",
                Sources = new List<RagSource>(),
                Meta = new RagResponseMeta
                {
                    Model = "test-model",
                    LatencyMs = 100,
                    GuardrailsApplied = true
                }
            });
    }
}
