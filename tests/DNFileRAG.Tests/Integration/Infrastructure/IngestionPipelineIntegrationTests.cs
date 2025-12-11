using DNFileRAG.Core.Configuration;
using DNFileRAG.Core.Interfaces;
using DNFileRAG.Core.Models;
using DNFileRAG.Infrastructure.Parsers;
using DNFileRAG.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace DNFileRAG.Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests for the IngestionPipeline service testing the full
/// document processing workflow with mocked external dependencies.
/// </summary>
public class IngestionPipelineIntegrationTests : IAsyncLifetime
{
    private readonly string _testDirectory;
    private readonly Mock<IVectorStore> _mockVectorStore;
    private readonly Mock<IEmbeddingProvider> _mockEmbeddingProvider;
    private IIngestionPipeline? _pipeline;

    public IngestionPipelineIntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"DNFileRAG_Ingestion_{Guid.NewGuid():N}");
        _mockVectorStore = new Mock<IVectorStore>();
        _mockEmbeddingProvider = new Mock<IEmbeddingProvider>();
    }

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_testDirectory);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        return Task.CompletedTask;
    }

    private IIngestionPipeline CreatePipeline()
    {
        var chunkingOptions = Options.Create(new ChunkingOptions
        {
            ChunkSize = 500,
            ChunkOverlap = 50
        });

        var fileWatcherOptions = Options.Create(new FileWatcherOptions
        {
            WatchPath = _testDirectory,
            SupportedExtensions = new[] { ".txt", ".md", ".pdf", ".docx", ".html" }
        });

        var textChunker = new TextChunker();
        var parserFactory = new DocumentParserFactory();

        // Setup default mock behaviors
        _mockEmbeddingProvider
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenerateRandomVector(128));

        _mockEmbeddingProvider
            .Setup(x => x.GenerateEmbeddingsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string> texts, CancellationToken _) =>
                texts.Select(_ => GenerateRandomVector(128)).ToList());

        _mockVectorStore
            .Setup(x => x.IsDocumentIndexedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockVectorStore
            .Setup(x => x.UpsertChunksAsync(It.IsAny<IEnumerable<DocumentChunk>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockVectorStore
            .Setup(x => x.DeleteByFileIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        _mockVectorStore
            .Setup(x => x.GetDocumentListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentInfo>());

        return new IngestionPipelineService(
            fileWatcherOptions,
            chunkingOptions,
            parserFactory,
            textChunker,
            _mockEmbeddingProvider.Object,
            _mockVectorStore.Object,
            NullLogger<IngestionPipelineService>.Instance);
    }

    [Fact]
    public async Task ProcessFileAsync_WithTextFile_ParsesAndIndexes()
    {
        // Arrange
        _pipeline = CreatePipeline();
        var testFile = Path.Combine(_testDirectory, "test-document.txt");
        await File.WriteAllTextAsync(testFile, "This is test content for the document. " +
            "It has multiple sentences to ensure proper chunking. " +
            "The pipeline should process this file successfully.");

        IEnumerable<DocumentChunk>? capturedChunks = null;
        _mockVectorStore
            .Setup(x => x.UpsertChunksAsync(It.IsAny<IEnumerable<DocumentChunk>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<DocumentChunk>, CancellationToken>((chunks, _) => capturedChunks = chunks.ToList())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _pipeline.ProcessFileAsync(testFile);

        // Assert
        result.Should().BeTrue("File should be processed");
        capturedChunks.Should().NotBeNull();
        capturedChunks.Should().NotBeEmpty();
        capturedChunks!.All(c => c.Metadata.FileName == "test-document.txt").Should().BeTrue();
        capturedChunks.All(c => c.Embedding.Length > 0).Should().BeTrue();
    }

    [Fact]
    public async Task ProcessFileAsync_WithMarkdownFile_ParsesCorrectly()
    {
        // Arrange
        _pipeline = CreatePipeline();
        var testFile = Path.Combine(_testDirectory, "readme.md");
        await File.WriteAllTextAsync(testFile, @"# Test Document

## Introduction
This is a markdown document with headers and formatting.

## Content
Here is some **bold** and *italic* text.");

        IEnumerable<DocumentChunk>? capturedChunks = null;
        _mockVectorStore
            .Setup(x => x.UpsertChunksAsync(It.IsAny<IEnumerable<DocumentChunk>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<DocumentChunk>, CancellationToken>((chunks, _) => capturedChunks = chunks.ToList())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _pipeline.ProcessFileAsync(testFile);

        // Assert
        result.Should().BeTrue();
        capturedChunks.Should().NotBeNull();
        capturedChunks!.All(c => c.Content.Length > 0).Should().BeTrue();
    }

    [Fact]
    public async Task ProcessFileAsync_SkipsUnchangedFile()
    {
        // Arrange
        _pipeline = CreatePipeline();
        var testFile = Path.Combine(_testDirectory, "unchanged.txt");
        await File.WriteAllTextAsync(testFile, "Content that won't change");

        // Mock that document is already indexed with same hash
        _mockVectorStore
            .Setup(x => x.IsDocumentIndexedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _pipeline.ProcessFileAsync(testFile);

        // Assert
        result.Should().BeFalse("Unchanged file should be skipped");
        _mockVectorStore.Verify(
            x => x.UpsertChunksAsync(It.IsAny<IEnumerable<DocumentChunk>>(), It.IsAny<CancellationToken>()),
            Times.Never, "No upsert should occur for unchanged file");
    }

    [Fact]
    public async Task ProcessFileAsync_GeneratesEmbeddingsForEachChunk()
    {
        // Arrange
        _pipeline = CreatePipeline();
        var testFile = Path.Combine(_testDirectory, "multi-chunk.txt");
        // Create content large enough to produce multiple chunks
        var content = string.Join(" ", Enumerable.Repeat(
            "This is a sentence that will repeat many times to create enough content for multiple chunks.", 50));
        await File.WriteAllTextAsync(testFile, content);

        var totalEmbeddingsGenerated = 0;

        // Override the batch embedding method AFTER CreatePipeline to track the number of embeddings generated
        _mockEmbeddingProvider
            .Setup(x => x.GenerateEmbeddingsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string> texts, CancellationToken _) =>
            {
                Interlocked.Add(ref totalEmbeddingsGenerated, texts.Count);
                return texts.Select(_ => GenerateRandomVector(128)).ToList();
            });

        // Act
        await _pipeline.ProcessFileAsync(testFile);

        // Assert
        totalEmbeddingsGenerated.Should().BeGreaterThan(1, "Multiple chunks should generate multiple embeddings");
    }

    [Fact]
    public async Task ProcessFileAsync_AssignsCorrectMetadata()
    {
        // Arrange
        _pipeline = CreatePipeline();
        var testFile = Path.Combine(_testDirectory, "metadata-test.txt");
        await File.WriteAllTextAsync(testFile, "Test content for metadata verification");

        IEnumerable<DocumentChunk>? capturedChunks = null;
        _mockVectorStore
            .Setup(x => x.UpsertChunksAsync(It.IsAny<IEnumerable<DocumentChunk>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<DocumentChunk>, CancellationToken>((chunks, _) => capturedChunks = chunks.ToList())
            .Returns(Task.CompletedTask);

        // Act
        await _pipeline.ProcessFileAsync(testFile);

        // Assert
        capturedChunks.Should().NotBeNull();
        var chunk = capturedChunks!.First();
        chunk.Metadata.FilePath.Should().Be(testFile);
        chunk.Metadata.FileName.Should().Be("metadata-test.txt");
        chunk.Metadata.FileId.Should().NotBeNullOrEmpty();
        chunk.Metadata.FileHash.Should().NotBeNullOrEmpty();
        chunk.Metadata.ChunkIndex.Should().Be(0);
        chunk.Metadata.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveFileAsync_DeletesFromVectorStore()
    {
        // Arrange
        _pipeline = CreatePipeline();
        var testFile = "/documents/deleted-file.txt";

        _mockVectorStore
            .Setup(x => x.DeleteByFileIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        // Act
        var result = await _pipeline.RemoveFileAsync(testFile);

        // Assert
        result.Should().Be(10);
        _mockVectorStore.Verify(
            x => x.DeleteByFileIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReindexAllAsync_ProcessesAllFilesInDirectory()
    {
        // Arrange
        _pipeline = CreatePipeline();

        // Create multiple test files
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "file1.txt"), "Content one");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "file2.txt"), "Content two");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "file3.md"), "Content three");

        var processedFiles = new List<string>();
        _mockVectorStore
            .Setup(x => x.UpsertChunksAsync(It.IsAny<IEnumerable<DocumentChunk>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<DocumentChunk>, CancellationToken>((chunks, _) =>
            {
                var first = chunks.FirstOrDefault();
                if (first != null)
                    processedFiles.Add(first.Metadata.FileName);
            })
            .Returns(Task.CompletedTask);

        // Act
        var count = await _pipeline.ReindexAllAsync();

        // Assert
        count.Should().Be(3);
        processedFiles.Should().Contain("file1.txt");
        processedFiles.Should().Contain("file2.txt");
        processedFiles.Should().Contain("file3.md");
    }

    [Fact]
    public async Task ReindexAllAsync_ProcessesSubdirectories()
    {
        // Arrange
        _pipeline = CreatePipeline();

        var subDir = Path.Combine(_testDirectory, "subdir");
        Directory.CreateDirectory(subDir);

        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "root.txt"), "Root content");
        await File.WriteAllTextAsync(Path.Combine(subDir, "nested.txt"), "Nested content");

        var processedFiles = new List<string>();
        _mockVectorStore
            .Setup(x => x.UpsertChunksAsync(It.IsAny<IEnumerable<DocumentChunk>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<DocumentChunk>, CancellationToken>((chunks, _) =>
            {
                var first = chunks.FirstOrDefault();
                if (first != null)
                    processedFiles.Add(first.Metadata.FileName);
            })
            .Returns(Task.CompletedTask);

        // Act
        var count = await _pipeline.ReindexAllAsync();

        // Assert
        count.Should().Be(2);
        processedFiles.Should().Contain("root.txt");
        processedFiles.Should().Contain("nested.txt");
    }

    [Fact]
    public async Task ReindexAllAsync_IgnoresUnsupportedExtensions()
    {
        // Arrange
        _pipeline = CreatePipeline();

        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "supported.txt"), "Good");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "unsupported.xyz"), "Bad");

        var processedFiles = new List<string>();
        _mockVectorStore
            .Setup(x => x.UpsertChunksAsync(It.IsAny<IEnumerable<DocumentChunk>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<DocumentChunk>, CancellationToken>((chunks, _) =>
            {
                var first = chunks.FirstOrDefault();
                if (first != null)
                    processedFiles.Add(first.Metadata.FileName);
            })
            .Returns(Task.CompletedTask);

        // Act
        var count = await _pipeline.ReindexAllAsync();

        // Assert
        count.Should().Be(1);
        processedFiles.Should().Contain("supported.txt");
        processedFiles.Should().NotContain("unsupported.xyz");
    }

    [Fact]
    public async Task ProcessFileAsync_HandlesHtmlFiles()
    {
        // Arrange
        _pipeline = CreatePipeline();
        var testFile = Path.Combine(_testDirectory, "page.html");
        await File.WriteAllTextAsync(testFile, @"<!DOCTYPE html>
<html>
<head><title>Test Page</title></head>
<body>
<h1>Welcome</h1>
<p>This is the main content of the page.</p>
<script>console.log('ignored');</script>
<style>.ignored { display: none; }</style>
</body>
</html>");

        IEnumerable<DocumentChunk>? capturedChunks = null;
        _mockVectorStore
            .Setup(x => x.UpsertChunksAsync(It.IsAny<IEnumerable<DocumentChunk>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<DocumentChunk>, CancellationToken>((chunks, _) => capturedChunks = chunks.ToList())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _pipeline.ProcessFileAsync(testFile);

        // Assert
        result.Should().BeTrue();
        capturedChunks.Should().NotBeNull();
        var content = string.Join(" ", capturedChunks!.Select(c => c.Content));
        content.Should().Contain("Welcome");
        content.Should().Contain("main content");
        content.Should().NotContain("console.log"); // Script should be removed
    }

    private float[] GenerateRandomVector(int size)
    {
        var random = new Random();
        return Enumerable.Range(0, size)
            .Select(_ => (float)random.NextDouble())
            .ToArray();
    }
}
