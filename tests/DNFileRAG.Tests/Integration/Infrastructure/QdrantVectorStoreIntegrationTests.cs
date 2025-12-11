using DNFileRAG.Core.Configuration;
using DNFileRAG.Core.Interfaces;
using DNFileRAG.Core.Models;
using DNFileRAG.Infrastructure.VectorStore;
using DNFileRAG.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DNFileRAG.Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests for QdrantVectorStore using a real Qdrant container.
/// </summary>
[Collection("Qdrant")]
public class QdrantVectorStoreIntegrationTests : IClassFixture<QdrantContainerFixture>, IAsyncLifetime
{
    private readonly QdrantContainerFixture _fixture;
    private IVectorStore _vectorStore = null!;
    private const int VectorSize = 128; // Small vectors for testing
    private readonly string _collectionName = $"test_{Guid.NewGuid():N}";

    public QdrantVectorStoreIntegrationTests(QdrantContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var options = Options.Create(new QdrantOptions
        {
            Host = _fixture.Host,
            Port = _fixture.HttpPort,
            CollectionName = _collectionName,
            VectorSize = VectorSize
        });

        var httpClient = new HttpClient();
        _vectorStore = new QdrantVectorStore(httpClient, options, NullLogger<QdrantVectorStore>.Instance);
        await _vectorStore.EnsureCollectionAsync();
    }

    public Task DisposeAsync()
    {
        // Collection cleanup is automatic when container stops
        return Task.CompletedTask;
    }

    [Fact]
    public async Task EnsureCollectionAsync_CreatesCollectionIfNotExists()
    {
        // Arrange - collection already created in InitializeAsync

        // Assert - we should be able to upsert without error
        var chunks = new[] { CreateTestChunk("test-file-1", 0) };
        await _vectorStore.UpsertChunksAsync(chunks);

        // Verify by retrieving document list
        var docs = await _vectorStore.GetDocumentListAsync();
        docs.Should().NotBeNull();
    }

    [Fact]
    public async Task UpsertChunksAsync_InsertsChunks()
    {
        // Arrange
        var chunks = new[]
        {
            CreateTestChunk("file-1", 0),
            CreateTestChunk("file-1", 1),
            CreateTestChunk("file-1", 2)
        };

        // Act
        await _vectorStore.UpsertChunksAsync(chunks);

        // Assert
        var isIndexed = await _vectorStore.IsDocumentIndexedAsync("file-1", "hash-file-1");
        isIndexed.Should().BeTrue();
    }

    [Fact]
    public async Task UpsertChunksAsync_UpdatesExistingChunks()
    {
        // Arrange
        var chunk = CreateTestChunk("update-file", 0);
        await _vectorStore.UpsertChunksAsync(new[] { chunk });

        // Act - upsert with same ID but different content
        var updatedChunk = new DocumentChunk
        {
            Id = chunk.Id,
            Content = "Updated content",
            Embedding = GenerateRandomVector(),
            Metadata = new ChunkMetadata
            {
                FileId = chunk.Metadata.FileId,
                FilePath = chunk.Metadata.FilePath,
                FileName = chunk.Metadata.FileName,
                FileHash = chunk.Metadata.FileHash,
                ChunkIndex = chunk.Metadata.ChunkIndex,
                PageNumber = chunk.Metadata.PageNumber,
                CreatedAt = chunk.Metadata.CreatedAt,
                UpdatedAt = DateTime.UtcNow,
                IsActive = chunk.Metadata.IsActive
            }
        };
        await _vectorStore.UpsertChunksAsync(new[] { updatedChunk });

        // Assert - search should return updated content
        var results = await _vectorStore.SearchAsync(updatedChunk.Embedding, 1);
        results.Should().HaveCount(1);
        results[0].Content.Should().Be("Updated content");
    }

    [Fact]
    public async Task DeleteByFileIdAsync_RemovesAllChunksForFile()
    {
        // Arrange
        var fileId = $"delete-test-{Guid.NewGuid():N}";
        var chunks = new[]
        {
            CreateTestChunk(fileId, 0),
            CreateTestChunk(fileId, 1),
            CreateTestChunk(fileId, 2)
        };
        await _vectorStore.UpsertChunksAsync(chunks);

        // Verify chunks exist
        var isIndexed = await _vectorStore.IsDocumentIndexedAsync(fileId, $"hash-{fileId}");
        isIndexed.Should().BeTrue();

        // Act
        var deletedCount = await _vectorStore.DeleteByFileIdAsync(fileId);

        // Assert
        deletedCount.Should().Be(3);
        var isStillIndexed = await _vectorStore.IsDocumentIndexedAsync(fileId, $"hash-{fileId}");
        isStillIndexed.Should().BeFalse();
    }

    [Fact]
    public async Task SearchAsync_ReturnsMatchingChunks()
    {
        // Arrange
        var chunks = new[]
        {
            CreateTestChunk("search-file", 0),
            CreateTestChunk("search-file", 1)
        };
        await _vectorStore.UpsertChunksAsync(chunks);

        // Act - search with similar vector to first chunk
        var results = await _vectorStore.SearchAsync(chunks[0].Embedding, 5);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(r => r.Score.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task SearchAsync_RespectsTopKLimit()
    {
        // Arrange
        var chunks = Enumerable.Range(0, 10)
            .Select(i => CreateTestChunk($"topk-file-{i}", 0))
            .ToArray();
        await _vectorStore.UpsertChunksAsync(chunks);

        // Act
        var results = await _vectorStore.SearchAsync(chunks[0].Embedding, 3);

        // Assert
        results.Should().HaveCountLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task GetDocumentListAsync_ReturnsIndexedDocuments()
    {
        // Arrange
        var file1Id = $"doc-list-1-{Guid.NewGuid():N}";
        var file2Id = $"doc-list-2-{Guid.NewGuid():N}";

        await _vectorStore.UpsertChunksAsync(new[]
        {
            CreateTestChunk(file1Id, 0),
            CreateTestChunk(file1Id, 1),
            CreateTestChunk(file2Id, 0)
        });

        // Act
        var docs = await _vectorStore.GetDocumentListAsync();

        // Assert
        docs.Should().Contain(d => d.FileId == file1Id);
        docs.Should().Contain(d => d.FileId == file2Id);

        var doc1 = docs.FirstOrDefault(d => d.FileId == file1Id);
        doc1.Should().NotBeNull();
        doc1!.ChunkCount.Should().Be(2);
    }

    [Fact]
    public async Task IsDocumentIndexedAsync_ReturnsTrueForIndexedDocument()
    {
        // Arrange
        var fileId = $"indexed-{Guid.NewGuid():N}";
        var fileHash = $"hash-{fileId}";
        await _vectorStore.UpsertChunksAsync(new[] { CreateTestChunk(fileId, 0) });

        // Act
        var isIndexed = await _vectorStore.IsDocumentIndexedAsync(fileId, fileHash);

        // Assert
        isIndexed.Should().BeTrue();
    }

    [Fact]
    public async Task IsDocumentIndexedAsync_ReturnsFalseForDifferentHash()
    {
        // Arrange
        var fileId = $"hash-check-{Guid.NewGuid():N}";
        await _vectorStore.UpsertChunksAsync(new[] { CreateTestChunk(fileId, 0) });

        // Act
        var isIndexed = await _vectorStore.IsDocumentIndexedAsync(fileId, "different-hash");

        // Assert
        isIndexed.Should().BeFalse();
    }

    [Fact]
    public async Task IsDocumentIndexedAsync_ReturnsFalseForNonExistentDocument()
    {
        // Act
        var isIndexed = await _vectorStore.IsDocumentIndexedAsync("non-existent", "any-hash");

        // Assert
        isIndexed.Should().BeFalse();
    }

    private DocumentChunk CreateTestChunk(string fileId, int chunkIndex)
    {
        return new DocumentChunk
        {
            Id = $"{fileId}_{chunkIndex}",
            Content = $"Test content for chunk {chunkIndex} of file {fileId}",
            Embedding = GenerateRandomVector(),
            Metadata = new ChunkMetadata
            {
                FileId = fileId,
                FilePath = $"/documents/{fileId}.txt",
                FileName = $"{fileId}.txt",
                FileHash = $"hash-{fileId}",
                ChunkIndex = chunkIndex,
                PageNumber = chunkIndex + 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            }
        };
    }

    private float[] GenerateRandomVector()
    {
        var random = new Random();
        return Enumerable.Range(0, VectorSize)
            .Select(_ => (float)random.NextDouble())
            .ToArray();
    }
}
