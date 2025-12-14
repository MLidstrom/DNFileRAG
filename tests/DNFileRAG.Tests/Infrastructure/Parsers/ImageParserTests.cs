using DNFileRAG.Core.Interfaces;
using DNFileRAG.Infrastructure.Parsers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace DNFileRAG.Tests.Infrastructure.Parsers;

public class ImageParserTests
{
    private readonly FakeVisionTextExtractor _defaultVision = new(new VisionTextResult
    {
        ExtractedText = "Hello World",
        Description = "A screenshot of a help desk page."
    });

    [Fact]
    public async Task ParseAsync_UsesVisionExtractor_AndBuildsIndexableContent()
    {
        // Arrange
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
        await File.WriteAllBytesAsync(tmp, new byte[] { 1, 2, 3, 4 }); // content irrelevant for this unit test

        try
        {
            var parser = new ImageParser(_defaultVision, NullLogger<ImageParser>.Instance);

            // Act
            var doc = await parser.ParseAsync(tmp);

            // Assert
            doc.Content.Should().Contain("Extracted text:");
            doc.Content.Should().Contain("Hello World");
            doc.Content.Should().Contain("Description:");
            doc.Content.Should().Contain("help desk page");
            doc.Metadata.Should().NotBeNull();
            doc.Metadata!.Title.Should().Be(Path.GetFileName(tmp));
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    [Theory]
    [InlineData(".png")]
    [InlineData(".PNG")]
    [InlineData(".jpg")]
    [InlineData(".JPG")]
    [InlineData(".jpeg")]
    [InlineData(".JPEG")]
    [InlineData(".webp")]
    [InlineData(".WEBP")]
    public void CanParse_SupportedExtensions_ReturnsTrue(string extension)
    {
        // Arrange
        var parser = new ImageParser(_defaultVision, NullLogger<ImageParser>.Instance);

        // Act & Assert
        parser.CanParse(extension).Should().BeTrue();
    }

    [Theory]
    [InlineData(".pdf")]
    [InlineData(".docx")]
    [InlineData(".txt")]
    [InlineData(".gif")]
    [InlineData(".bmp")]
    public void CanParse_UnsupportedExtensions_ReturnsFalse(string extension)
    {
        // Arrange
        var parser = new ImageParser(_defaultVision, NullLogger<ImageParser>.Instance);

        // Act & Assert
        parser.CanParse(extension).Should().BeFalse();
    }

    [Fact]
    public void SupportedExtensions_ReturnsAllImageFormats()
    {
        // Arrange
        var parser = new ImageParser(_defaultVision, NullLogger<ImageParser>.Instance);

        // Act & Assert
        parser.SupportedExtensions.Should().BeEquivalentTo([".png", ".jpg", ".jpeg", ".webp"]);
    }

    [Fact]
    public async Task ParseAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var parser = new ImageParser(_defaultVision, NullLogger<ImageParser>.Instance);
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");

        // Act & Assert
        await parser.Invoking(p => p.ParseAsync(nonExistentPath))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public void CanParse_NullExtension_ThrowsArgumentNullException()
    {
        // Arrange
        var parser = new ImageParser(_defaultVision, NullLogger<ImageParser>.Instance);

        // Act & Assert
        parser.Invoking(p => p.CanParse(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ParseAsync_NullFilePath_ThrowsArgumentNullException()
    {
        // Arrange
        var parser = new ImageParser(_defaultVision, NullLogger<ImageParser>.Instance);

        // Act & Assert
        await parser.Invoking(p => p.ParseAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ParseAsync_EmptyVisionResult_OmitsEmptySections()
    {
        // Arrange
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
        await File.WriteAllBytesAsync(tmp, new byte[] { 1, 2, 3, 4 });

        try
        {
            var emptyVision = new FakeVisionTextExtractor(new VisionTextResult
            {
                ExtractedText = string.Empty,
                Description = string.Empty
            });

            var parser = new ImageParser(emptyVision, NullLogger<ImageParser>.Instance);

            // Act
            var doc = await parser.ParseAsync(tmp);

            // Assert
            doc.Content.Should().NotContain("Extracted text:");
            doc.Content.Should().NotContain("Description:");
            doc.Content.Should().Contain("Image:"); // Header is always present
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    [Fact]
    public async Task ParseAsync_OnlyDescription_IncludesDescriptionOnly()
    {
        // Arrange
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jpg");
        await File.WriteAllBytesAsync(tmp, new byte[] { 1, 2, 3, 4 });

        try
        {
            var descOnlyVision = new FakeVisionTextExtractor(new VisionTextResult
            {
                ExtractedText = string.Empty,
                Description = "A beautiful sunset over the ocean."
            });

            var parser = new ImageParser(descOnlyVision, NullLogger<ImageParser>.Instance);

            // Act
            var doc = await parser.ParseAsync(tmp);

            // Assert
            doc.Content.Should().NotContain("Extracted text:");
            doc.Content.Should().Contain("Description:");
            doc.Content.Should().Contain("beautiful sunset");
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    private sealed class FakeVisionTextExtractor : IVisionTextExtractor
    {
        private readonly VisionTextResult _result;

        public FakeVisionTextExtractor(VisionTextResult result) => _result = result;

        public Task<VisionTextResult> ExtractAsync(byte[] imageBytes, string? fileName = null, CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }
}
