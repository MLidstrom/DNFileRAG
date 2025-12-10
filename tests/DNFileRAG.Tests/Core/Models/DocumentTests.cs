using DNFileRAG.Core.Models;
using FluentAssertions;

namespace DNFileRAG.Tests.Core.Models;

public class DocumentTests
{
    [Fact]
    public void Document_WithRequiredProperties_ShouldInitialize()
    {
        // Arrange & Act
        var document = new Document
        {
            FileId = "abc123",
            FilePath = "/documents/test.pdf",
            FileName = "test.pdf",
            FileHash = "hash123"
        };

        // Assert
        document.FileId.Should().Be("abc123");
        document.FilePath.Should().Be("/documents/test.pdf");
        document.FileName.Should().Be("test.pdf");
        document.FileHash.Should().Be("hash123");
    }

    [Fact]
    public void Document_ShouldHaveDefaultTimestamps()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var document = new Document
        {
            FileId = "abc123",
            FilePath = "/documents/test.pdf",
            FileName = "test.pdf",
            FileHash = "hash123"
        };

        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        document.CreatedAt.Should().BeAfter(beforeCreation);
        document.CreatedAt.Should().BeBefore(afterCreation);
        document.UpdatedAt.Should().BeAfter(beforeCreation);
        document.UpdatedAt.Should().BeBefore(afterCreation);
    }

    [Fact]
    public void Document_ChunkCount_ShouldDefaultToZero()
    {
        // Arrange & Act
        var document = new Document
        {
            FileId = "abc123",
            FilePath = "/documents/test.pdf",
            FileName = "test.pdf",
            FileHash = "hash123"
        };

        // Assert
        document.ChunkCount.Should().Be(0);
    }

    [Fact]
    public void Document_ChunkCount_ShouldBeSettable()
    {
        // Arrange
        var document = new Document
        {
            FileId = "abc123",
            FilePath = "/documents/test.pdf",
            FileName = "test.pdf",
            FileHash = "hash123"
        };

        // Act
        document.ChunkCount = 42;

        // Assert
        document.ChunkCount.Should().Be(42);
    }
}
