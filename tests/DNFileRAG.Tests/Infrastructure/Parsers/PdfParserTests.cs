using DNFileRAG.Infrastructure.Parsers;
using FluentAssertions;

namespace DNFileRAG.Tests.Infrastructure.Parsers;

public class PdfParserTests
{
    private readonly PdfParser _parser = new();

    #region SupportedExtensions Tests

    [Fact]
    public void SupportedExtensions_ShouldContainPdf()
    {
        _parser.SupportedExtensions.Should().Contain(".pdf");
        _parser.SupportedExtensions.Should().HaveCount(1);
    }

    #endregion

    #region CanParse Tests

    [Theory]
    [InlineData(".pdf", true)]
    [InlineData(".PDF", true)]
    [InlineData(".txt", false)]
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

    #region ParseAsync Validation Tests

    [Fact]
    public async Task ParseAsync_WithNullFilePath_ShouldThrowArgumentNullException()
    {
        var act = async () => await _parser.ParseAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ParseAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        var act = async () => await _parser.ParseAsync("/nonexistent/file.pdf");
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ParseAsync_WithInvalidPdfFile_ShouldThrowException()
    {
        // Arrange - create a file that is not a valid PDF
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
        try
        {
            await File.WriteAllTextAsync(tempFile, "This is not a PDF file");

            // Act & Assert
            var act = async () => await _parser.ParseAsync(tempFile);
            await act.Should().ThrowAsync<Exception>(); // PdfPig throws various exceptions for invalid PDFs
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    #endregion
}
