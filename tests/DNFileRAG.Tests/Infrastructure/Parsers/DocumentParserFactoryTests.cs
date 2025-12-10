using DNFileRAG.Core.Interfaces;
using DNFileRAG.Infrastructure.Parsers;
using FluentAssertions;

namespace DNFileRAG.Tests.Infrastructure.Parsers;

public class DocumentParserFactoryTests
{
    private readonly DocumentParserFactory _factory = new();

    #region Constructor Tests

    [Fact]
    public void Constructor_WithoutParsers_ShouldCreateDefaultParsers()
    {
        // Assert
        _factory.SupportedExtensions.Should().Contain(".txt");
        _factory.SupportedExtensions.Should().Contain(".md");
        _factory.SupportedExtensions.Should().Contain(".html");
        _factory.SupportedExtensions.Should().Contain(".htm");
        _factory.SupportedExtensions.Should().Contain(".pdf");
        _factory.SupportedExtensions.Should().Contain(".docx");
    }

    [Fact]
    public void Constructor_WithCustomParsers_ShouldUseProvidedParsers()
    {
        // Arrange
        var customParser = new PlainTextParser();
        var factory = new DocumentParserFactory([customParser]);

        // Assert
        factory.SupportedExtensions.Should().BeEquivalentTo([".txt", ".md"]);
    }

    #endregion

    #region GetParser Tests

    [Theory]
    [InlineData(".txt")]
    [InlineData(".md")]
    [InlineData(".html")]
    [InlineData(".htm")]
    [InlineData(".pdf")]
    [InlineData(".docx")]
    public void GetParser_WithSupportedExtension_ShouldReturnParser(string extension)
    {
        var parser = _factory.GetParser(extension);
        parser.Should().NotBeNull();
        parser!.CanParse(extension).Should().BeTrue();
    }

    [Theory]
    [InlineData(".TXT")]
    [InlineData(".PDF")]
    [InlineData(".HTML")]
    public void GetParser_WithUppercaseExtension_ShouldReturnParser(string extension)
    {
        var parser = _factory.GetParser(extension);
        parser.Should().NotBeNull();
    }

    [Theory]
    [InlineData(".xyz")]
    [InlineData(".doc")]
    [InlineData(".csv")]
    public void GetParser_WithUnsupportedExtension_ShouldReturnNull(string extension)
    {
        var parser = _factory.GetParser(extension);
        parser.Should().BeNull();
    }

    [Fact]
    public void GetParser_WithNullExtension_ShouldThrowArgumentNullException()
    {
        var act = () => _factory.GetParser(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region GetParserForFile Tests

    [Theory]
    [InlineData("/path/to/file.txt")]
    [InlineData("/path/to/file.md")]
    [InlineData("/path/to/file.html")]
    [InlineData("/path/to/file.pdf")]
    [InlineData("/path/to/file.docx")]
    public void GetParserForFile_WithSupportedFile_ShouldReturnParser(string filePath)
    {
        var parser = _factory.GetParserForFile(filePath);
        parser.Should().NotBeNull();
    }

    [Fact]
    public void GetParserForFile_WithUnsupportedFile_ShouldReturnNull()
    {
        var parser = _factory.GetParserForFile("/path/to/file.xyz");
        parser.Should().BeNull();
    }

    [Fact]
    public void GetParserForFile_WithNoExtension_ShouldReturnNull()
    {
        var parser = _factory.GetParserForFile("/path/to/filenoext");
        parser.Should().BeNull();
    }

    [Fact]
    public void GetParserForFile_WithNullFilePath_ShouldThrowArgumentNullException()
    {
        var act = () => _factory.GetParserForFile(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region CanParse Tests

    [Theory]
    [InlineData(".txt", true)]
    [InlineData(".md", true)]
    [InlineData(".html", true)]
    [InlineData(".pdf", true)]
    [InlineData(".docx", true)]
    [InlineData(".xyz", false)]
    [InlineData(".doc", false)]
    public void CanParse_ShouldReturnCorrectValue(string extension, bool expected)
    {
        _factory.CanParse(extension).Should().Be(expected);
    }

    [Fact]
    public void CanParse_WithNullExtension_ShouldThrowArgumentNullException()
    {
        var act = () => _factory.CanParse(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region CanParseFile Tests

    [Theory]
    [InlineData("/file.txt", true)]
    [InlineData("/file.pdf", true)]
    [InlineData("/file.xyz", false)]
    [InlineData("/filenoext", false)]
    public void CanParseFile_ShouldReturnCorrectValue(string filePath, bool expected)
    {
        _factory.CanParseFile(filePath).Should().Be(expected);
    }

    [Fact]
    public void CanParseFile_WithNullFilePath_ShouldThrowArgumentNullException()
    {
        var act = () => _factory.CanParseFile(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region SupportedExtensions Tests

    [Fact]
    public void SupportedExtensions_ShouldReturnDistinctExtensions()
    {
        var extensions = _factory.SupportedExtensions.ToList();
        extensions.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void SupportedExtensions_ShouldContainAllDefaultExtensions()
    {
        var extensions = _factory.SupportedExtensions.ToList();
        extensions.Should().HaveCountGreaterThanOrEqualTo(6); // txt, md, html, htm, pdf, docx
    }

    #endregion

    #region Parser Type Tests

    [Fact]
    public void GetParser_ForTxt_ShouldReturnPlainTextParser()
    {
        var parser = _factory.GetParser(".txt");
        parser.Should().BeOfType<PlainTextParser>();
    }

    [Fact]
    public void GetParser_ForHtml_ShouldReturnHtmlParser()
    {
        var parser = _factory.GetParser(".html");
        parser.Should().BeOfType<HtmlParser>();
    }

    [Fact]
    public void GetParser_ForPdf_ShouldReturnPdfParser()
    {
        var parser = _factory.GetParser(".pdf");
        parser.Should().BeOfType<PdfParser>();
    }

    [Fact]
    public void GetParser_ForDocx_ShouldReturnDocxParser()
    {
        var parser = _factory.GetParser(".docx");
        parser.Should().BeOfType<DocxParser>();
    }

    #endregion
}
