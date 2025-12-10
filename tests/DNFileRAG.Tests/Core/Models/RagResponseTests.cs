using DNFileRAG.Core.Models;
using FluentAssertions;

namespace DNFileRAG.Tests.Core.Models;

public class RagResponseTests
{
    [Fact]
    public void RagResponse_WithRequiredProperties_ShouldInitialize()
    {
        // Arrange
        var sources = new List<RagSource>
        {
            new()
            {
                FilePath = "/docs/test.pdf",
                FileName = "test.pdf",
                ChunkIndex = 0,
                Score = 0.95f
            }
        };

        var meta = new RagResponseMeta
        {
            GuardrailsApplied = true,
            Model = "gpt-4",
            LatencyMs = 250
        };

        // Act
        var response = new RagResponse
        {
            Answer = "The answer is 42.",
            Sources = sources,
            Meta = meta
        };

        // Assert
        response.Answer.Should().Be("The answer is 42.");
        response.Sources.Should().HaveCount(1);
        response.Meta.Should().Be(meta);
    }

    [Fact]
    public void RagSource_WithRequiredProperties_ShouldInitialize()
    {
        // Arrange & Act
        var source = new RagSource
        {
            FilePath = "/docs/manual.pdf",
            FileName = "manual.pdf",
            ChunkIndex = 5,
            Score = 0.87f
        };

        // Assert
        source.FilePath.Should().Be("/docs/manual.pdf");
        source.FileName.Should().Be("manual.pdf");
        source.ChunkIndex.Should().Be(5);
        source.Score.Should().Be(0.87f);
    }

    [Fact]
    public void RagSource_OptionalProperties_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var source = new RagSource
        {
            FilePath = "/docs/test.pdf",
            FileName = "test.pdf",
            ChunkIndex = 0,
            Score = 0.9f
        };

        // Assert
        source.PageNumber.Should().BeNull();
        source.Content.Should().BeNull();
    }

    [Fact]
    public void RagSource_WithOptionalProperties_ShouldInitialize()
    {
        // Arrange & Act
        var source = new RagSource
        {
            FilePath = "/docs/test.pdf",
            FileName = "test.pdf",
            ChunkIndex = 2,
            Score = 0.92f,
            PageNumber = 15,
            Content = "This is the chunk content..."
        };

        // Assert
        source.PageNumber.Should().Be(15);
        source.Content.Should().Be("This is the chunk content...");
    }

    [Fact]
    public void RagResponseMeta_WithRequiredProperties_ShouldInitialize()
    {
        // Arrange & Act
        var meta = new RagResponseMeta
        {
            GuardrailsApplied = true,
            Model = "claude-3-sonnet",
            LatencyMs = 500
        };

        // Assert
        meta.GuardrailsApplied.Should().BeTrue();
        meta.Model.Should().Be("claude-3-sonnet");
        meta.LatencyMs.Should().Be(500);
    }

    [Fact]
    public void RagResponseMeta_ConversationId_ShouldBeOptional()
    {
        // Arrange & Act
        var metaWithConvo = new RagResponseMeta
        {
            Model = "gpt-4",
            LatencyMs = 100,
            ConversationId = "session-abc"
        };

        var metaWithoutConvo = new RagResponseMeta
        {
            Model = "gpt-4",
            LatencyMs = 100
        };

        // Assert
        metaWithConvo.ConversationId.Should().Be("session-abc");
        metaWithoutConvo.ConversationId.Should().BeNull();
    }

    [Fact]
    public void RagResponse_WithMultipleSources_ShouldPreserveOrder()
    {
        // Arrange
        var sources = new List<RagSource>
        {
            new() { FilePath = "/a.pdf", FileName = "a.pdf", ChunkIndex = 0, Score = 0.95f },
            new() { FilePath = "/b.pdf", FileName = "b.pdf", ChunkIndex = 1, Score = 0.90f },
            new() { FilePath = "/c.pdf", FileName = "c.pdf", ChunkIndex = 2, Score = 0.85f }
        };

        // Act
        var response = new RagResponse
        {
            Answer = "Combined answer",
            Sources = sources,
            Meta = new RagResponseMeta { Model = "test", LatencyMs = 0 }
        };

        // Assert
        response.Sources.Should().HaveCount(3);
        response.Sources[0].FilePath.Should().Be("/a.pdf");
        response.Sources[1].FilePath.Should().Be("/b.pdf");
        response.Sources[2].FilePath.Should().Be("/c.pdf");
    }
}
