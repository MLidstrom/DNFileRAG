using DNFileRAG.Core.Models;
using FluentAssertions;

namespace DNFileRAG.Tests.Core.Models;

public class ParsedDocumentTests
{
    [Fact]
    public void ParsedDocument_WithRequiredProperties_ShouldInitialize()
    {
        // Arrange & Act
        var doc = new ParsedDocument
        {
            Content = "Full document content here"
        };

        // Assert
        doc.Content.Should().Be("Full document content here");
    }

    [Fact]
    public void ParsedDocument_Pages_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var doc = new ParsedDocument
        {
            Content = "Content"
        };

        // Assert
        doc.Pages.Should().BeNull();
    }

    [Fact]
    public void ParsedDocument_WithPages_ShouldInitialize()
    {
        // Arrange
        var pages = new List<PageContent>
        {
            new() { PageNumber = 1, Content = "Page 1 content" },
            new() { PageNumber = 2, Content = "Page 2 content" },
            new() { PageNumber = 3, Content = "Page 3 content" }
        };

        // Act
        var doc = new ParsedDocument
        {
            Content = "Full content",
            Pages = pages
        };

        // Assert
        doc.Pages.Should().HaveCount(3);
        doc.Pages![0].PageNumber.Should().Be(1);
        doc.Pages![1].PageNumber.Should().Be(2);
        doc.Pages![2].PageNumber.Should().Be(3);
    }

    [Fact]
    public void PageContent_WithRequiredProperties_ShouldInitialize()
    {
        // Arrange & Act
        var page = new PageContent
        {
            PageNumber = 42,
            Content = "This is page 42"
        };

        // Assert
        page.PageNumber.Should().Be(42);
        page.Content.Should().Be("This is page 42");
    }

    [Fact]
    public void ParsedDocument_Metadata_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var doc = new ParsedDocument
        {
            Content = "Content"
        };

        // Assert
        doc.Metadata.Should().BeNull();
    }

    [Fact]
    public void ParsedDocument_WithMetadata_ShouldInitialize()
    {
        // Arrange
        var metadata = new DocumentParseMetadata
        {
            Title = "My Document",
            Author = "John Doe",
            CreatedDate = new DateTime(2024, 1, 15),
            PageCount = 10
        };

        // Act
        var doc = new ParsedDocument
        {
            Content = "Content",
            Metadata = metadata
        };

        // Assert
        doc.Metadata.Should().NotBeNull();
        doc.Metadata!.Title.Should().Be("My Document");
        doc.Metadata!.Author.Should().Be("John Doe");
        doc.Metadata!.CreatedDate.Should().Be(new DateTime(2024, 1, 15));
        doc.Metadata!.PageCount.Should().Be(10);
    }

    [Fact]
    public void DocumentParseMetadata_AllProperties_ShouldBeNullable()
    {
        // Arrange & Act
        var metadata = new DocumentParseMetadata();

        // Assert
        metadata.Title.Should().BeNull();
        metadata.Author.Should().BeNull();
        metadata.CreatedDate.Should().BeNull();
        metadata.PageCount.Should().BeNull();
    }
}
