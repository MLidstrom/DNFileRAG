using DNFileRAG.Infrastructure.Parsers;
using FluentAssertions;

namespace DNFileRAG.Tests.Infrastructure.Parsers;

public class PlainTextParserTests : IDisposable
{
    private readonly PlainTextParser _parser = new();
    private readonly List<string> _tempFiles = [];

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    private string CreateTempFile(string extension, string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");
        File.WriteAllText(path, content);
        _tempFiles.Add(path);
        return path;
    }

    #region SupportedExtensions Tests

    [Fact]
    public void SupportedExtensions_ShouldContainTxtAndMd()
    {
        _parser.SupportedExtensions.Should().Contain(".txt");
        _parser.SupportedExtensions.Should().Contain(".md");
    }

    #endregion

    #region CanParse Tests

    [Theory]
    [InlineData(".txt", true)]
    [InlineData(".TXT", true)]
    [InlineData(".md", true)]
    [InlineData(".MD", true)]
    [InlineData(".pdf", false)]
    [InlineData(".docx", false)]
    [InlineData(".html", false)]
    public void CanParse_ShouldReturnCorrectValue(string extension, bool expected)
    {
        _parser.CanParse(extension).Should().Be(expected);
    }

    [Fact]
    public void CanParse_WithNullExtension_ShouldThrowArgumentNullException()
    {
        var act = () => _parser.CanParse(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ParseAsync Tests

    [Fact]
    public async Task ParseAsync_WithNullFilePath_ShouldThrowArgumentNullException()
    {
        var act = async () => await _parser.ParseAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ParseAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        var act = async () => await _parser.ParseAsync("/nonexistent/file.txt");
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ParseAsync_WithTextFile_ShouldReturnContent()
    {
        // Arrange
        var content = "Hello, this is a test file.\nWith multiple lines.";
        var filePath = CreateTempFile(".txt", content);

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        result.Content.Should().Be(content);
        result.Pages.Should().BeNull();
    }

    [Fact]
    public async Task ParseAsync_WithMarkdownFile_ShouldReturnContent()
    {
        // Arrange
        var content = "# Heading\n\nThis is **markdown** content.";
        var filePath = CreateTempFile(".md", content);

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        result.Content.Should().Be(content);
    }

    [Fact]
    public async Task ParseAsync_WithEmptyFile_ShouldReturnEmptyContent()
    {
        // Arrange
        var filePath = CreateTempFile(".txt", "");

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        result.Content.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_ShouldIncludeMetadata()
    {
        // Arrange
        var filePath = CreateTempFile(".txt", "Test content");

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        result.Metadata.Should().NotBeNull();
        result.Metadata!.CreatedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task ParseAsync_WithUnicodeContent_ShouldPreserveContent()
    {
        // Arrange
        var content = "Hello 世界! Привет мир! مرحبا بالعالم";
        var filePath = CreateTempFile(".txt", content);

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        result.Content.Should().Be(content);
    }

    [Fact]
    public async Task ParseAsync_ShouldSupportCancellation()
    {
        // Arrange
        var filePath = CreateTempFile(".txt", "Test");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await _parser.ParseAsync(filePath, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion
}
