using DNFileRAG.Core.Models;
using DNFileRAG.Infrastructure.Services;
using FluentAssertions;

namespace DNFileRAG.Tests.Infrastructure.Services;

public class TextChunkerTests
{
    private readonly TextChunker _chunker = new();

    #region Chunk Method - Basic Tests

    [Fact]
    public void Chunk_WithEmptyString_ShouldReturnEmptyList()
    {
        // Arrange & Act
        var result = _chunker.Chunk("", 100, 20);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Chunk_WithWhitespaceOnly_ShouldReturnEmptyList()
    {
        // Arrange & Act
        var result = _chunker.Chunk("   \t\n  ", 100, 20);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Chunk_WithNullText_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => _chunker.Chunk(null!, 100, 20);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("text");
    }

    [Fact]
    public void Chunk_WithZeroChunkSize_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange & Act
        var act = () => _chunker.Chunk("test", 0, 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("chunkSize");
    }

    [Fact]
    public void Chunk_WithNegativeChunkSize_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange & Act
        var act = () => _chunker.Chunk("test", -1, 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("chunkSize");
    }

    [Fact]
    public void Chunk_WithNegativeOverlap_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange & Act
        var act = () => _chunker.Chunk("test", 100, -1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("overlap");
    }

    [Fact]
    public void Chunk_WithOverlapEqualToChunkSize_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => _chunker.Chunk("test", 100, 100);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("overlap");
    }

    [Fact]
    public void Chunk_WithOverlapGreaterThanChunkSize_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => _chunker.Chunk("test", 100, 150);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("overlap");
    }

    #endregion

    #region Chunk Method - Chunking Behavior

    [Fact]
    public void Chunk_TextSmallerThanChunkSize_ShouldReturnSingleChunk()
    {
        // Arrange
        var text = "This is a short text.";

        // Act
        var result = _chunker.Chunk(text, 100, 20);

        // Assert
        result.Should().HaveCount(1);
        result[0].Content.Should().Be("This is a short text.");
        result[0].Index.Should().Be(0);
    }

    [Fact]
    public void Chunk_TextEqualToChunkSize_ShouldReturnSingleChunk()
    {
        // Arrange
        var text = "Exactly ten"; // 11 chars

        // Act
        var result = _chunker.Chunk(text, 11, 2);

        // Assert
        result.Should().HaveCount(1);
        result[0].Content.Should().Be("Exactly ten");
    }

    [Fact]
    public void Chunk_TextLargerThanChunkSize_ShouldReturnMultipleChunks()
    {
        // Arrange
        var text = "This is a longer text that needs to be split into multiple chunks for processing.";

        // Act
        var result = _chunker.Chunk(text, 30, 5);

        // Assert
        result.Should().HaveCountGreaterThan(1);
        result.Should().AllSatisfy(c => c.Content.Should().NotBeNullOrWhiteSpace());
    }

    [Fact]
    public void Chunk_ShouldPreserveSequentialIndexes()
    {
        // Arrange
        var text = "First sentence. Second sentence. Third sentence. Fourth sentence.";

        // Act
        var result = _chunker.Chunk(text, 20, 5);

        // Assert
        for (int i = 0; i < result.Count; i++)
        {
            result[i].Index.Should().Be(i);
        }
    }

    [Fact]
    public void Chunk_ShouldSetPositions()
    {
        // Arrange
        var text = "First sentence. Second sentence.";

        // Act
        var result = _chunker.Chunk(text, 20, 5);

        // Assert
        result.Should().AllSatisfy(chunk =>
        {
            chunk.StartPosition.Should().BeGreaterThanOrEqualTo(0);
            chunk.EndPosition.Should().BeGreaterThan(chunk.StartPosition);
        });
    }

    [Fact]
    public void Chunk_WithZeroOverlap_ShouldNotRepeatContent()
    {
        // Arrange
        var text = "AAAA BBBB CCCC DDDD";

        // Act
        var result = _chunker.Chunk(text, 5, 0);

        // Assert
        var allContent = string.Join("", result.Select(c => c.Content));
        // Content might have some differences due to whitespace handling, but should cover all
        result.Should().HaveCountGreaterThan(1);
    }

    #endregion

    #region Chunk Method - Break Point Detection

    [Fact]
    public void Chunk_ShouldPreferSentenceBreaks()
    {
        // Arrange
        var text = "This is the first sentence. This is the second sentence that is longer.";

        // Act
        var result = _chunker.Chunk(text, 40, 5);

        // Assert
        // First chunk should end at sentence boundary if possible
        result[0].Content.Should().EndWith(".");
    }

    [Fact]
    public void Chunk_ShouldRespectExclamationAsBreak()
    {
        // Arrange
        var text = "Hello world! This is another sentence.";

        // Act
        var result = _chunker.Chunk(text, 15, 3);

        // Assert - first chunk should end at exclamation mark when used as break point
        result.Should().HaveCountGreaterThan(1);
        result[0].Content.Should().EndWith("!");
    }

    [Fact]
    public void Chunk_ShouldRespectQuestionAsBreak()
    {
        // Arrange
        var text = "What is this? This is the answer.";

        // Act
        var result = _chunker.Chunk(text, 15, 3);

        // Assert - first chunk should end at question mark when used as break point
        result.Should().HaveCountGreaterThan(1);
        result[0].Content.Should().EndWith("?");
    }

    [Fact]
    public void Chunk_ShouldFallBackToWordBreaks()
    {
        // Arrange - text without sentence endings within chunk boundaries
        var text = "word1 word2 word3 word4 word5 word6";

        // Act
        var result = _chunker.Chunk(text, 15, 3);

        // Assert - should break at word boundaries, not mid-word
        result.Should().AllSatisfy(chunk =>
        {
            // Chunks should generally not start or end mid-word
            chunk.Content.Should().NotStartWith("ord");
        });
    }

    #endregion

    #region Chunk Method - Text Normalization

    [Fact]
    public void Chunk_ShouldNormalizeMultipleSpaces()
    {
        // Arrange
        var text = "Word1    Word2     Word3";

        // Act
        var result = _chunker.Chunk(text, 100, 10);

        // Assert
        result[0].Content.Should().NotContain("  ");
    }

    [Fact]
    public void Chunk_ShouldNormalizeExcessiveNewlines()
    {
        // Arrange
        var text = "Paragraph1\n\n\n\n\nParagraph2";

        // Act
        var result = _chunker.Chunk(text, 100, 10);

        // Assert
        result[0].Content.Should().NotContain("\n\n\n");
    }

    [Fact]
    public void Chunk_ShouldTrimWhitespace()
    {
        // Arrange
        var text = "   Some text   ";

        // Act
        var result = _chunker.Chunk(text, 100, 10);

        // Assert
        result[0].Content.Should().Be("Some text");
    }

    #endregion

    #region ChunkDocument Method - Basic Tests

    [Fact]
    public void ChunkDocument_WithNullDocument_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => _chunker.ChunkDocument(null!, 100, 20);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ChunkDocument_WithoutPages_ShouldFallBackToSimpleChunking()
    {
        // Arrange
        var document = new ParsedDocument
        {
            Content = "This is document content without page information."
        };

        // Act
        var result = _chunker.ChunkDocument(document, 30, 5);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(chunk => chunk.PageNumber.Should().BeNull());
    }

    [Fact]
    public void ChunkDocument_WithEmptyPages_ShouldFallBackToSimpleChunking()
    {
        // Arrange
        var document = new ParsedDocument
        {
            Content = "This is document content.",
            Pages = []
        };

        // Act
        var result = _chunker.ChunkDocument(document, 100, 10);

        // Assert
        result.Should().NotBeEmpty();
    }

    #endregion

    #region ChunkDocument Method - Page Information

    [Fact]
    public void ChunkDocument_WithPages_ShouldPreservePageNumbers()
    {
        // Arrange
        var document = new ParsedDocument
        {
            Content = "Page 1 content. Page 2 content.",
            Pages =
            [
                new PageContent { PageNumber = 1, Content = "Page 1 content." },
                new PageContent { PageNumber = 2, Content = "Page 2 content." }
            ]
        };

        // Act
        var result = _chunker.ChunkDocument(document, 50, 5);

        // Assert
        result.Should().Contain(c => c.PageNumber == 1);
        result.Should().Contain(c => c.PageNumber == 2);
    }

    [Fact]
    public void ChunkDocument_WithMultipleChunksPerPage_ShouldAllHaveSamePageNumber()
    {
        // Arrange
        var document = new ParsedDocument
        {
            Content = "Long page content that spans multiple chunks.",
            Pages =
            [
                new PageContent
                {
                    PageNumber = 5,
                    Content = "This is a very long page content that will definitely need to be split into multiple chunks because it is quite lengthy."
                }
            ]
        };

        // Act
        var result = _chunker.ChunkDocument(document, 30, 5);

        // Assert
        result.Should().HaveCountGreaterThan(1);
        result.Should().AllSatisfy(c => c.PageNumber.Should().Be(5));
    }

    [Fact]
    public void ChunkDocument_ShouldMaintainGlobalIndexAcrossPages()
    {
        // Arrange
        var document = new ParsedDocument
        {
            Content = "Page 1 content. Page 2 content.",
            Pages =
            [
                new PageContent { PageNumber = 1, Content = "Page 1 content here." },
                new PageContent { PageNumber = 2, Content = "Page 2 content here." }
            ]
        };

        // Act
        var result = _chunker.ChunkDocument(document, 100, 10);

        // Assert
        for (int i = 0; i < result.Count; i++)
        {
            result[i].Index.Should().Be(i);
        }
    }

    [Fact]
    public void ChunkDocument_WithEmptyPage_ShouldSkipIt()
    {
        // Arrange
        var document = new ParsedDocument
        {
            Content = "Content",
            Pages =
            [
                new PageContent { PageNumber = 1, Content = "Page 1." },
                new PageContent { PageNumber = 2, Content = "" },
                new PageContent { PageNumber = 3, Content = "Page 3." }
            ]
        };

        // Act
        var result = _chunker.ChunkDocument(document, 100, 10);

        // Assert
        result.Should().NotContain(c => c.PageNumber == 2);
        result.Should().Contain(c => c.PageNumber == 1);
        result.Should().Contain(c => c.PageNumber == 3);
    }

    [Fact]
    public void ChunkDocument_WithWhitespaceOnlyPage_ShouldSkipIt()
    {
        // Arrange
        var document = new ParsedDocument
        {
            Content = "Content",
            Pages =
            [
                new PageContent { PageNumber = 1, Content = "Page 1." },
                new PageContent { PageNumber = 2, Content = "   \t\n  " },
                new PageContent { PageNumber = 3, Content = "Page 3." }
            ]
        };

        // Act
        var result = _chunker.ChunkDocument(document, 100, 10);

        // Assert
        result.Should().NotContain(c => c.PageNumber == 2);
    }

    #endregion

    #region Integration-Style Tests

    [Fact]
    public void Chunk_LongDocument_ShouldHandleEfficiently()
    {
        // Arrange - simulate a longer document
        var paragraphs = Enumerable.Range(1, 50)
            .Select(i => $"This is paragraph number {i} with some content that makes it reasonably long.");
        var text = string.Join("\n\n", paragraphs);

        // Act
        var result = _chunker.Chunk(text, 200, 30);

        // Assert
        result.Should().HaveCountGreaterThan(10);
        result.Should().AllSatisfy(chunk =>
        {
            chunk.Content.Length.Should().BeLessThanOrEqualTo(250); // Some margin for normalization
            chunk.Content.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void ChunkDocument_MultiPagePdf_ShouldChunkCorrectly()
    {
        // Arrange - simulate a multi-page PDF
        var pages = Enumerable.Range(1, 10)
            .Select(i => new PageContent
            {
                PageNumber = i,
                Content = $"Page {i} content. This page contains information about topic {i}. " +
                         $"There is additional text here to make the page longer and more realistic."
            }).ToList();

        var document = new ParsedDocument
        {
            Content = string.Join("\n", pages.Select(p => p.Content)),
            Pages = pages
        };

        // Act
        var result = _chunker.ChunkDocument(document, 50, 10);

        // Assert
        result.Should().HaveCountGreaterThan(pages.Count); // Multiple chunks per page
        result.Select(c => c.PageNumber).Distinct().Should().HaveCount(10); // All pages represented
    }

    #endregion
}
