using DNFileRAG.Core.Models;
using FluentAssertions;

namespace DNFileRAG.Tests.Core.Models;

public class TextChunkTests
{
    [Fact]
    public void TextChunk_WithRequiredProperties_ShouldInitialize()
    {
        // Arrange & Act
        var chunk = new TextChunk
        {
            Content = "This is the chunk content.",
            Index = 0
        };

        // Assert
        chunk.Content.Should().Be("This is the chunk content.");
        chunk.Index.Should().Be(0);
    }

    [Fact]
    public void TextChunk_PageNumber_ShouldBeNullable()
    {
        // Arrange & Act
        var chunkWithPage = new TextChunk
        {
            Content = "Page content",
            Index = 0,
            PageNumber = 5
        };

        var chunkWithoutPage = new TextChunk
        {
            Content = "Content",
            Index = 0
        };

        // Assert
        chunkWithPage.PageNumber.Should().Be(5);
        chunkWithoutPage.PageNumber.Should().BeNull();
    }

    [Fact]
    public void TextChunk_Positions_ShouldDefaultToZero()
    {
        // Arrange & Act
        var chunk = new TextChunk
        {
            Content = "Content",
            Index = 0
        };

        // Assert
        chunk.StartPosition.Should().Be(0);
        chunk.EndPosition.Should().Be(0);
    }

    [Fact]
    public void TextChunk_Positions_ShouldBeSettable()
    {
        // Arrange & Act
        var chunk = new TextChunk
        {
            Content = "Content in the middle of document",
            Index = 5,
            StartPosition = 1000,
            EndPosition = 1500
        };

        // Assert
        chunk.StartPosition.Should().Be(1000);
        chunk.EndPosition.Should().Be(1500);
    }

    [Fact]
    public void TextChunk_Index_ShouldSupportSequentialValues()
    {
        // Arrange & Act
        var chunks = Enumerable.Range(0, 10).Select(i => new TextChunk
        {
            Content = $"Chunk {i}",
            Index = i
        }).ToList();

        // Assert
        for (int i = 0; i < 10; i++)
        {
            chunks[i].Index.Should().Be(i);
            chunks[i].Content.Should().Be($"Chunk {i}");
        }
    }
}
