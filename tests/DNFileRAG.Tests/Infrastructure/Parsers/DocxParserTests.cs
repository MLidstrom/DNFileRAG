using DNFileRAG.Infrastructure.Parsers;
using FluentAssertions;

namespace DNFileRAG.Tests.Infrastructure.Parsers;

public class DocxParserTests
{
    private readonly DocxParser _parser = new();

    #region SupportedExtensions Tests

    [Fact]
    public void SupportedExtensions_ShouldContainDocx()
    {
        _parser.SupportedExtensions.Should().Contain(".docx");
        _parser.SupportedExtensions.Should().HaveCount(1);
    }

    #endregion

    #region CanParse Tests

    [Theory]
    [InlineData(".docx", true)]
    [InlineData(".DOCX", true)]
    [InlineData(".doc", false)]
    [InlineData(".txt", false)]
    [InlineData(".pdf", false)]
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
        var act = async () => await _parser.ParseAsync("/nonexistent/file.docx");
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ParseAsync_WithInvalidDocxFile_ShouldThrowException()
    {
        // Arrange - create a file that is not a valid DOCX
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.docx");
        try
        {
            await File.WriteAllTextAsync(tempFile, "This is not a DOCX file");

            // Act & Assert
            var act = async () => await _parser.ParseAsync(tempFile);
            await act.Should().ThrowAsync<Exception>(); // OpenXml throws for invalid files
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    #endregion
}
