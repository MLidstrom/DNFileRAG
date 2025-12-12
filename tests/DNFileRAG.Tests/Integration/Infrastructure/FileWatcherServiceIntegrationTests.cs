using DNFileRAG.Core.Configuration;
using DNFileRAG.Core.Interfaces;
using DNFileRAG.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace DNFileRAG.Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests for FileWatcherService using real file system operations.
/// </summary>
[Trait("Category", "Integration")]
public class FileWatcherServiceIntegrationTests : IAsyncLifetime
{
    private readonly string _testDirectory;
    private readonly Mock<IIngestionPipeline> _mockIngestionPipeline;
    private FileWatcherService? _service;
    private CancellationTokenSource? _cts;

    public FileWatcherServiceIntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"DNFileRAG_Tests_{Guid.NewGuid():N}");
        _mockIngestionPipeline = new Mock<IIngestionPipeline>();
    }

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_testDirectory);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _cts?.Cancel();
        _service?.Dispose();

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

    private FileWatcherService CreateService(int debounceMs = 100)
    {
        var options = Options.Create(new FileWatcherOptions
        {
            WatchPath = _testDirectory,
            IncludeSubdirectories = true,
            SupportedExtensions = new[] { ".txt", ".md", ".pdf" },
            DebounceMilliseconds = debounceMs
        });

        _mockIngestionPipeline
            .Setup(x => x.ReindexAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        return new FileWatcherService(
            options,
            _mockIngestionPipeline.Object,
            NullLogger<FileWatcherService>.Instance);
    }

    [Fact]
    public async Task FileWatcher_DetectsNewFile()
    {
        // Arrange
        _service = CreateService();
        _cts = new CancellationTokenSource();

        var processFileCalled = new TaskCompletionSource<string>();
        _mockIngestionPipeline
            .Setup(x => x.ProcessFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((path, _) => processFileCalled.TrySetResult(path))
            .ReturnsAsync(true);

        // Start the service
        _ = _service.StartAsync(_cts.Token);
        await Task.Delay(500); // Allow service to initialize

        // Act - create a new file
        var testFile = Path.Combine(_testDirectory, "test-new-file.txt");
        await File.WriteAllTextAsync(testFile, "Test content");

        // Assert - wait for the file to be processed
        var completedTask = await Task.WhenAny(
            processFileCalled.Task,
            Task.Delay(TimeSpan.FromSeconds(5)));

        completedTask.Should().Be(processFileCalled.Task, "File should have been detected and processed");
        var processedPath = await processFileCalled.Task;
        processedPath.Should().Be(testFile);
    }

    [Fact]
    public async Task FileWatcher_DetectsModifiedFile()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "test-modify.txt");
        await File.WriteAllTextAsync(testFile, "Initial content");

        _service = CreateService();
        _cts = new CancellationTokenSource();

        var processFileCalled = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var modificationWritten = false;
        _mockIngestionPipeline
            .Setup(x => x.ProcessFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((path, _) =>
            {
                if (modificationWritten && string.Equals(path, testFile, StringComparison.OrdinalIgnoreCase))
                {
                    processFileCalled.TrySetResult(path);
                }
            })
            .ReturnsAsync(true);

        // Start the service
        _ = _service.StartAsync(_cts.Token);
        await Task.Delay(250); // Allow service to initialize and set up watcher

        // Act - modify the file
        modificationWritten = true;
        await File.WriteAllTextAsync(testFile, "Modified content");

        // Assert
        var completedTask = await Task.WhenAny(
            processFileCalled.Task,
            Task.Delay(TimeSpan.FromSeconds(5)));

        completedTask.Should().Be(processFileCalled.Task, "Modified file should have been detected");
    }

    [Fact]
    public async Task FileWatcher_DetectsDeletedFile()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "test-delete.txt");
        await File.WriteAllTextAsync(testFile, "Content to delete");

        _service = CreateService();
        _cts = new CancellationTokenSource();

        var removeFileCalled = new TaskCompletionSource<string>();
        _mockIngestionPipeline
            .Setup(x => x.RemoveFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((path, _) => removeFileCalled.TrySetResult(path))
            .ReturnsAsync(1);

        // Start the service
        _ = _service.StartAsync(_cts.Token);
        await Task.Delay(500); // Allow service to initialize

        // Act - delete the file
        File.Delete(testFile);

        // Assert
        var completedTask = await Task.WhenAny(
            removeFileCalled.Task,
            Task.Delay(TimeSpan.FromSeconds(5)));

        completedTask.Should().Be(removeFileCalled.Task, "Deleted file should have been detected");
        var removedPath = await removeFileCalled.Task;
        removedPath.Should().Be(testFile);
    }

    [Fact]
    public async Task FileWatcher_IgnoresUnsupportedExtensions()
    {
        // Arrange
        _service = CreateService();
        _cts = new CancellationTokenSource();

        var processFileCalled = false;
        _mockIngestionPipeline
            .Setup(x => x.ProcessFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((path, _) =>
            {
                if (path.EndsWith(".unsupported"))
                    processFileCalled = true;
            })
            .ReturnsAsync(true);

        // Start the service
        _ = _service.StartAsync(_cts.Token);
        await Task.Delay(500); // Allow service to initialize

        // Act - create a file with unsupported extension
        var testFile = Path.Combine(_testDirectory, "test.unsupported");
        await File.WriteAllTextAsync(testFile, "Test content");

        // Wait a bit to see if it gets processed
        await Task.Delay(1000);

        // Assert
        processFileCalled.Should().BeFalse("Unsupported file extension should be ignored");
    }

    [Fact]
    public async Task FileWatcher_PerformsInitialIndexing()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "existing-file.txt");
        await File.WriteAllTextAsync(testFile, "Existing content");

        _service = CreateService();
        _cts = new CancellationTokenSource();

        var reindexCalled = new TaskCompletionSource<bool>();
        _mockIngestionPipeline
            .Setup(x => x.ReindexAllAsync(It.IsAny<CancellationToken>()))
            .Callback(() => reindexCalled.TrySetResult(true))
            .ReturnsAsync(1);

        // Act - start the service
        _ = _service.StartAsync(_cts.Token);

        // Assert - verify ReindexAllAsync was called
        var completedTask = await Task.WhenAny(
            reindexCalled.Task,
            Task.Delay(TimeSpan.FromSeconds(5)));

        completedTask.Should().Be(reindexCalled.Task, "Initial indexing should have been performed");
    }

    [Fact]
    public async Task FileWatcher_HandlesSubdirectories()
    {
        // Arrange
        var subDir = Path.Combine(_testDirectory, "subdirectory");
        Directory.CreateDirectory(subDir);

        _service = CreateService();
        _cts = new CancellationTokenSource();

        var processFileCalled = new TaskCompletionSource<string>();
        _mockIngestionPipeline
            .Setup(x => x.ProcessFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((path, _) => processFileCalled.TrySetResult(path))
            .ReturnsAsync(true);

        // Start the service
        _ = _service.StartAsync(_cts.Token);
        await Task.Delay(500); // Allow service to initialize

        // Act - create a file in subdirectory
        var testFile = Path.Combine(subDir, "nested-file.txt");
        await File.WriteAllTextAsync(testFile, "Nested content");

        // Assert
        var completedTask = await Task.WhenAny(
            processFileCalled.Task,
            Task.Delay(TimeSpan.FromSeconds(5)));

        completedTask.Should().Be(processFileCalled.Task, "File in subdirectory should be detected");
        var processedPath = await processFileCalled.Task;
        processedPath.Should().Be(testFile);
    }

    [Fact]
    public async Task FileWatcher_DebouncesDuplicateEvents()
    {
        // Arrange
        _service = CreateService(debounceMs: 200);
        _cts = new CancellationTokenSource();

        var processCount = 0;
        _mockIngestionPipeline
            .Setup(x => x.ProcessFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((_, _) => Interlocked.Increment(ref processCount))
            .ReturnsAsync(true);

        // Start the service
        _ = _service.StartAsync(_cts.Token);
        await Task.Delay(500); // Allow service to initialize

        // Act - create file and modify it rapidly
        var testFile = Path.Combine(_testDirectory, "rapid-changes.txt");
        await File.WriteAllTextAsync(testFile, "Content 1");
        await Task.Delay(50);
        await File.WriteAllTextAsync(testFile, "Content 2");
        await Task.Delay(50);
        await File.WriteAllTextAsync(testFile, "Content 3");

        // Wait for debounce to settle
        await Task.Delay(1000);

        // Assert - should be debounced to fewer calls than 3
        processCount.Should().BeLessThanOrEqualTo(2, "Rapid changes should be debounced");
    }
}
