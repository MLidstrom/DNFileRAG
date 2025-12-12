using System.Collections.Concurrent;
using DNFileRAG.Core.Configuration;
using DNFileRAG.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DNFileRAG.Infrastructure.Services;

/// <summary>
/// Background service that watches for file changes in the configured directory
/// and queues files for processing through the ingestion pipeline.
/// </summary>
public class FileWatcherService : BackgroundService
{
    private readonly FileWatcherOptions _options;
    private readonly IIngestionPipeline _ingestionPipeline;
    private readonly ILogger<FileWatcherService> _logger;

    private FileSystemWatcher? _watcher;
    private readonly ConcurrentDictionary<string, DateTime> _pendingChanges = new();
    private readonly HashSet<string> _supportedExtensions;

    public FileWatcherService(
        IOptions<FileWatcherOptions> options,
        IIngestionPipeline ingestionPipeline,
        ILogger<FileWatcherService> logger)
    {
        _options = options.Value;
        _ingestionPipeline = ingestionPipeline;
        _logger = logger;

        _supportedExtensions = _options.SupportedExtensions
            .Select(e => e.ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FileWatcherService starting, watching path: {WatchPath}", _options.WatchPath);

        // Ensure the watch directory exists
        if (!Directory.Exists(_options.WatchPath))
        {
            _logger.LogWarning("Watch path does not exist, creating: {WatchPath}", _options.WatchPath);
            Directory.CreateDirectory(_options.WatchPath);
        }

        // Perform initial indexing
        _logger.LogInformation("Performing initial indexing of existing files");
        try
        {
            var count = await _ingestionPipeline.ReindexAllAsync(stoppingToken);
            _logger.LogInformation("Initial indexing complete, processed {Count} documents", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during initial indexing");
        }

        // Set up file watcher
        _watcher = new FileSystemWatcher(_options.WatchPath)
        {
            IncludeSubdirectories = _options.IncludeSubdirectories,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        _watcher.Created += OnFileCreated;
        _watcher.Changed += OnFileChanged;
        _watcher.Deleted += OnFileDeleted;
        _watcher.Renamed += OnFileRenamed;
        _watcher.Error += OnError;

        _logger.LogInformation("File watcher started for path: {WatchPath}", _options.WatchPath);

        // Process debounced changes
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingChangesAsync(stoppingToken);
            await Task.Delay(100, stoppingToken);
        }
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (IsSupportedFile(e.FullPath))
        {
            _logger.LogDebug("File created: {FilePath}", e.FullPath);
            QueueFileChange(e.FullPath);
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (IsSupportedFile(e.FullPath))
        {
            _logger.LogDebug("File changed: {FilePath}", e.FullPath);
            QueueFileChange(e.FullPath);
        }
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (IsSupportedFile(e.FullPath))
        {
            _logger.LogDebug("File deleted: {FilePath}", e.FullPath);
            // Remove from pending changes if queued
            _pendingChanges.TryRemove(e.FullPath, out _);

            // Queue deletion for processing
            Task.Run(async () =>
            {
                try
                {
                    var removed = await _ingestionPipeline.RemoveFileAsync(e.FullPath);
                    _logger.LogInformation("Removed {Count} vectors for deleted file: {FilePath}", removed, e.FullPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing file from index: {FilePath}", e.FullPath);
                }
            });
        }
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        // Handle old file as deleted
        if (IsSupportedFile(e.OldFullPath))
        {
            _logger.LogDebug("File renamed from: {OldPath} to {NewPath}", e.OldFullPath, e.FullPath);
            _pendingChanges.TryRemove(e.OldFullPath, out _);

            Task.Run(async () =>
            {
                try
                {
                    await _ingestionPipeline.RemoveFileAsync(e.OldFullPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing renamed file from index: {FilePath}", e.OldFullPath);
                }
            });
        }

        // Handle new file as created
        if (IsSupportedFile(e.FullPath))
        {
            QueueFileChange(e.FullPath);
        }
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        _logger.LogError(e.GetException(), "FileSystemWatcher error");
    }

    private void QueueFileChange(string filePath)
    {
        _pendingChanges[filePath] = DateTime.UtcNow;
    }

    private async Task ProcessPendingChangesAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var debounceTime = TimeSpan.FromMilliseconds(_options.DebounceMilliseconds);
        var filesToProcess = new List<string>();

        foreach (var kvp in _pendingChanges)
        {
            if (now - kvp.Value >= debounceTime)
            {
                if (_pendingChanges.TryRemove(kvp.Key, out _))
                {
                    filesToProcess.Add(kvp.Key);
                }
            }
        }

        foreach (var filePath in filesToProcess)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // Ensure file still exists (might have been deleted)
                if (!File.Exists(filePath))
                {
                    _logger.LogDebug("Skipping file that no longer exists: {FilePath}", filePath);
                    continue;
                }

                var processed = await _ingestionPipeline.ProcessFileAsync(filePath, cancellationToken);
                if (processed)
                {
                    _logger.LogInformation("Processed file: {FilePath}", filePath);
                }
                else
                {
                    _logger.LogDebug("File unchanged, skipped: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file: {FilePath}", filePath);
            }
        }
    }

    private bool IsSupportedFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return !string.IsNullOrEmpty(extension) &&
               _supportedExtensions.Contains(extension.ToLowerInvariant());
    }

    public override void Dispose()
    {
        _watcher?.Dispose();
        base.Dispose();
    }
}
