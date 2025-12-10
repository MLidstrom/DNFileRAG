using DNFileRAG.Core.Models;
using FluentAssertions;

namespace DNFileRAG.Tests.Core.Models;

public class RagQueryTests
{
    [Fact]
    public void RagQuery_WithRequiredProperties_ShouldInitialize()
    {
        // Arrange & Act
        var query = new RagQuery
        {
            Query = "What is the meaning of life?"
        };

        // Assert
        query.Query.Should().Be("What is the meaning of life?");
    }

    [Fact]
    public void RagQuery_OptionalProperties_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var query = new RagQuery
        {
            Query = "Test query"
        };

        // Assert
        query.TopK.Should().BeNull();
        query.Temperature.Should().BeNull();
        query.MaxTokens.Should().BeNull();
        query.Filters.Should().BeNull();
        query.ConversationId.Should().BeNull();
    }

    [Fact]
    public void RagQuery_WithAllProperties_ShouldInitialize()
    {
        // Arrange
        var filters = new RagQueryFilters
        {
            FilePaths = ["/docs/"],
            Tags = ["important", "internal"]
        };

        // Act
        var query = new RagQuery
        {
            Query = "Test query",
            TopK = 10,
            Temperature = 0.5f,
            MaxTokens = 1024,
            Filters = filters,
            ConversationId = "conv-123"
        };

        // Assert
        query.Query.Should().Be("Test query");
        query.TopK.Should().Be(10);
        query.Temperature.Should().Be(0.5f);
        query.MaxTokens.Should().Be(1024);
        query.Filters.Should().Be(filters);
        query.ConversationId.Should().Be("conv-123");
    }

    [Fact]
    public void RagQueryFilters_ShouldSupportMultiplePaths()
    {
        // Arrange & Act
        var filters = new RagQueryFilters
        {
            FilePaths = ["/docs/", "/manuals/", "/guides/"]
        };

        // Assert
        filters.FilePaths.Should().HaveCount(3);
        filters.FilePaths.Should().Contain("/docs/");
        filters.FilePaths.Should().Contain("/manuals/");
        filters.FilePaths.Should().Contain("/guides/");
    }

    [Fact]
    public void RagQueryFilters_ShouldSupportMultipleTags()
    {
        // Arrange & Act
        var filters = new RagQueryFilters
        {
            Tags = ["confidential", "v2", "approved"]
        };

        // Assert
        filters.Tags.Should().HaveCount(3);
        filters.Tags.Should().Contain("confidential");
        filters.Tags.Should().Contain("v2");
        filters.Tags.Should().Contain("approved");
    }
}
