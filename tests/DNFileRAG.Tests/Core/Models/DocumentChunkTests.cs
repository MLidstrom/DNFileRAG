using DNFileRAG.Core.Models;
using FluentAssertions;

namespace DNFileRAG.Tests.Core.Models;

public class DocumentChunkTests
{
    [Fact]
    public void DocumentChunk_WithRequiredProperties_ShouldInitialize()
    {
        // Arrange
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        var metadata = CreateTestMetadata();

        // Act
        var chunk = new DocumentChunk
        {
            Id = "chunk-001",
            Content = "This is test content",
            Embedding = embedding,
            Metadata = metadata
        };

        // Assert
        chunk.Id.Should().Be("chunk-001");
        chunk.Content.Should().Be("This is test content");
        chunk.Embedding.Should().BeEquivalentTo(embedding);
        chunk.Metadata.Should().Be(metadata);
    }

    [Fact]
    public void ChunkMetadata_WithRequiredProperties_ShouldInitialize()
    {
        // Arrange & Act
        var metadata = new ChunkMetadata
        {
            FileId = "file123",
            FilePath = "/docs/test.pdf",
            FileName = "test.pdf",
            FileHash = "hash456",
            ChunkIndex = 5
        };

        // Assert
        metadata.FileId.Should().Be("file123");
        metadata.FilePath.Should().Be("/docs/test.pdf");
        metadata.FileName.Should().Be("test.pdf");
        metadata.FileHash.Should().Be("hash456");
        metadata.ChunkIndex.Should().Be(5);
    }

    [Fact]
    public void ChunkMetadata_PageNumber_ShouldBeNullable()
    {
        // Arrange & Act
        var metadataWithPage = new ChunkMetadata
        {
            FileId = "file123",
            FilePath = "/docs/test.pdf",
            FileName = "test.pdf",
            FileHash = "hash456",
            ChunkIndex = 0,
            PageNumber = 3
        };

        var metadataWithoutPage = new ChunkMetadata
        {
            FileId = "file123",
            FilePath = "/docs/test.txt",
            FileName = "test.txt",
            FileHash = "hash789",
            ChunkIndex = 0
        };

        // Assert
        metadataWithPage.PageNumber.Should().Be(3);
        metadataWithoutPage.PageNumber.Should().BeNull();
    }

    [Fact]
    public void ChunkMetadata_IsActive_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var metadata = CreateTestMetadata();

        // Assert
        metadata.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ChunkMetadata_ShouldHaveDefaultTimestamps()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var metadata = CreateTestMetadata();

        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        metadata.CreatedAt.Should().BeAfter(beforeCreation);
        metadata.CreatedAt.Should().BeBefore(afterCreation);
        metadata.UpdatedAt.Should().BeAfter(beforeCreation);
        metadata.UpdatedAt.Should().BeBefore(afterCreation);
    }

    private static ChunkMetadata CreateTestMetadata() => new()
    {
        FileId = "file123",
        FilePath = "/docs/test.pdf",
        FileName = "test.pdf",
        FileHash = "hash456",
        ChunkIndex = 0
    };
}
