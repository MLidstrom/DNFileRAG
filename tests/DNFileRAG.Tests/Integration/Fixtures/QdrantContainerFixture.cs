using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace DNFileRAG.Tests.Integration.Fixtures;

/// <summary>
/// Test fixture that provides a Qdrant container for integration tests.
/// </summary>
public class QdrantContainerFixture : IAsyncLifetime
{
    private IContainer? _container;

    /// <summary>
    /// The host address of the Qdrant container.
    /// </summary>
    public string Host => _container?.Hostname ?? "localhost";

    /// <summary>
    /// The mapped HTTP port for Qdrant.
    /// </summary>
    public int HttpPort => _container?.GetMappedPublicPort(6333) ?? 6333;

    /// <summary>
    /// The mapped gRPC port for Qdrant.
    /// </summary>
    public int GrpcPort => _container?.GetMappedPublicPort(6334) ?? 6334;

    /// <summary>
    /// Full HTTP URL for connecting to Qdrant.
    /// </summary>
    public string HttpUrl => $"http://{Host}:{HttpPort}";

    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder()
            .WithImage("qdrant/qdrant:latest")
            .WithPortBinding(6333, true)
            .WithPortBinding(6334, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r
                    .ForPath("/health")
                    .ForPort(6333)))
            .Build();

        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
