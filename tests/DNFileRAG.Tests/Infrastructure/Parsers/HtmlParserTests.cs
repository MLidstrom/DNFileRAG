using DNFileRAG.Infrastructure.Parsers;
using FluentAssertions;

namespace DNFileRAG.Tests.Infrastructure.Parsers;

public class HtmlParserTests : IDisposable
{
    private readonly HtmlParser _parser = new();
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
    public void SupportedExtensions_ShouldContainHtmlAndHtm()
    {
        _parser.SupportedExtensions.Should().Contain(".html");
        _parser.SupportedExtensions.Should().Contain(".htm");
    }

    #endregion

    #region CanParse Tests

    [Theory]
    [InlineData(".html", true)]
    [InlineData(".HTML", true)]
    [InlineData(".htm", true)]
    [InlineData(".HTM", true)]
    [InlineData(".pdf", false)]
    [InlineData(".txt", false)]
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
        var act = async () => await _parser.ParseAsync("/nonexistent/file.html");
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ParseAsync_WithSimpleHtml_ShouldExtractText()
    {
        // Arrange
        var html = "<html><body><p>Hello World</p></body></html>";
        var filePath = CreateTempFile(".html", html);

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        result.Content.Should().Contain("Hello World");
    }

    [Fact]
    public async Task ParseAsync_ShouldExtractTitleFromTitleTag()
    {
        // Arrange
        var html = "<html><head><title>My Page Title</title></head><body>Content</body></html>";
        var filePath = CreateTempFile(".html", html);

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        result.Metadata?.Title.Should().Be("My Page Title");
    }

    [Fact]
    public async Task ParseAsync_ShouldExtractTitleFromOgTitle()
    {
        // Arrange
        var html = "<html><head><meta property='og:title' content='OG Title'/></head><body>Content</body></html>";
        var filePath = CreateTempFile(".html", html);

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        result.Metadata?.Title.Should().Be("OG Title");
    }

    [Fact]
    public async Task ParseAsync_ShouldExtractTitleFromH1()
    {
        // Arrange
        var html = "<html><body><h1>Main Heading</h1><p>Content</p></body></html>";
        var filePath = CreateTempFile(".html", html);

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        result.Metadata?.Title.Should().Be("Main Heading");
    }

    [Fact]
    public async Task ParseAsync_ShouldRemoveScriptContent()
    {
        // Arrange
        var html = "<html><body><p>Visible</p><script>var x = 'Hidden';</script></body></html>";
        var filePath = CreateTempFile(".html", html);

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        result.Content.Should().Contain("Visible");
        result.Content.Should().NotContain("Hidden");
        result.Content.Should().NotContain("var x");
    }

    [Fact]
    public async Task ParseAsync_ShouldRemoveStyleContent()
    {
        // Arrange
        var html = "<html><head><style>.hidden { display: none; }</style></head><body>Visible</body></html>";
        var filePath = CreateTempFile(".html", html);

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        result.Content.Should().Contain("Visible");
        result.Content.Should().NotContain("display: none");
    }

    [Fact]
    public async Task ParseAsync_ShouldDecodeHtmlEntities()
    {
        // Arrange
        var html = "<html><body><p>Hello &amp; World &lt;tag&gt;</p></body></html>";
        var filePath = CreateTempFile(".html", html);

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        result.Content.Should().Contain("Hello & World <tag>");
    }

    [Fact]
    public async Task ParseAsync_ShouldNormalizeWhitespace()
    {
        // Arrange
        var html = "<html><body><p>Hello    \n\n   World</p></body></html>";
        var filePath = CreateTempFile(".html", html);

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        result.Content.Should().NotContain("    ");
        result.Content.Should().NotContain("\n\n");
    }

    [Fact]
    public async Task ParseAsync_WithComplexHtml_ShouldExtractAllText()
    {
        // Arrange
        var html = @"
            <html>
            <head><title>Test Page</title></head>
            <body>
                <header><nav>Navigation</nav></header>
                <main>
                    <h1>Main Content</h1>
                    <p>Paragraph one.</p>
                    <p>Paragraph two.</p>
                </main>
                <footer>Footer text</footer>
            </body>
            </html>";
        var filePath = CreateTempFile(".html", html);

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        result.Content.Should().Contain("Navigation");
        result.Content.Should().Contain("Main Content");
        result.Content.Should().Contain("Paragraph one");
        result.Content.Should().Contain("Paragraph two");
        result.Content.Should().Contain("Footer text");
    }

    [Fact]
    public async Task ParseAsync_ShouldIncludeMetadataWithCreatedDate()
    {
        // Arrange
        var filePath = CreateTempFile(".html", "<html><body>Test</body></html>");

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        result.Metadata.Should().NotBeNull();
        result.Metadata!.CreatedDate.Should().NotBeNull();
    }

    #endregion
}
