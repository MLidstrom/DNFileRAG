using System.Security.Cryptography;
using System.Text;
using DNFileRAG.Core.Configuration;
using DNFileRAG.Core.Interfaces;
using DNFileRAG.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DNFileRAG.Infrastructure.Services;

/// <summary>
/// Service for processing documents through the ingestion pipeline:
/// parsing, chunking, embedding generation, and vector storage.
/// </summary>
public class IngestionPipelineService : IIngestionPipeline
{
    private readonly FileWatcherOptions _fileWatcherOptions;
    private readonly ChunkingOptions _chunkingOptions;
    private readonly IDocumentParserFactory _parserFactory;
    private readonly ITextChunker _textChunker;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<IngestionPipelineService> _logger;

    public IngestionPipelineService(
        IOptions<FileWatcherOptions> fileWatcherOptions,
        IOptions<ChunkingOptions> chunkingOptions,
        IDocumentParserFactory parserFactory,
        ITextChunker textChunker,
        IEmbeddingProvider embeddingProvider,
        IVectorStore vectorStore,
        ILogger<IngestionPipelineService> logger)
    {
        _fileWatcherOptions = fileWatcherOptions.Value;
        _chunkingOptions = chunkingOptions.Value;
        _parserFactory = parserFactory;
        _textChunker = textChunker;
        _embeddingProvider = embeddingProvider;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> ProcessFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        _logger.LogDebug("Processing file: {FilePath}", filePath);

        // Generate file identifiers
        var fileId = GenerateFileId(filePath);
        var fileHash = await ComputeFileHashAsync(filePath, cancellationToken);

        // Check if already indexed with same hash
        if (await _vectorStore.IsDocumentIndexedAsync(fileId, fileHash, cancellationToken))
        {
            _logger.LogDebug("File already indexed with same hash, skipping: {FilePath}", filePath);
            return false;
        }

        // Get appropriate parser
        var parser = _parserFactory.GetParserForFile(filePath);
        if (parser == null)
        {
            _logger.LogWarning("No parser available for file: {FilePath}", filePath);
            return false;
        }

        // Parse document
        _logger.LogDebug("Parsing document: {FilePath}", filePath);
        var parsedDocument = await parser.ParseAsync(filePath, cancellationToken);

        if (string.IsNullOrWhiteSpace(parsedDocument.Content))
        {
            _logger.LogWarning("Document has no content: {FilePath}", filePath);
            return false;
        }

        // Chunk document
        _logger.LogDebug("Chunking document: {FilePath}", filePath);
        var textChunks = _textChunker.ChunkDocument(
            parsedDocument,
            _chunkingOptions.ChunkSize,
            _chunkingOptions.ChunkOverlap);

        if (textChunks.Count == 0)
        {
            _logger.LogWarning("Document produced no chunks: {FilePath}", filePath);
            return false;
        }

        // Generate embeddings
        _logger.LogDebug("Generating {Count} embeddings for: {FilePath}", textChunks.Count, filePath);
        var texts = textChunks.Select(c => c.Content).ToList();
        var embeddings = await _embeddingProvider.GenerateEmbeddingsAsync(texts, cancellationToken);

        // Delete existing vectors for this file
        await _vectorStore.DeleteByFileIdAsync(fileId, cancellationToken);

        // Create document chunks with embeddings
        var fileName = Path.GetFileName(filePath);
        var now = DateTime.UtcNow;
        var documentChunks = new List<DocumentChunk>();

        for (var i = 0; i < textChunks.Count; i++)
        {
            var chunk = textChunks[i];
            documentChunks.Add(new DocumentChunk
            {
                Id = Guid.NewGuid().ToString(),
                Content = chunk.Content,
                Embedding = embeddings[i],
                Metadata = new ChunkMetadata
                {
                    FileId = fileId,
                    FilePath = filePath,
                    FileName = fileName,
                    FileHash = fileHash,
                    ChunkIndex = i,
                    PageNumber = chunk.PageNumber,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsActive = true
                }
            });
        }

        // Upsert to vector store
        _logger.LogDebug("Upserting {Count} chunks to vector store: {FilePath}", documentChunks.Count, filePath);
        await _vectorStore.UpsertChunksAsync(documentChunks, cancellationToken);

        _logger.LogInformation("Successfully processed file with {Count} chunks: {FilePath}", documentChunks.Count, filePath);
        return true;
    }

    /// <inheritdoc />
    public async Task<int> RemoveFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        var fileId = GenerateFileId(filePath);
        var removed = await _vectorStore.DeleteByFileIdAsync(fileId, cancellationToken);

        _logger.LogInformation("Removed {Count} vectors for file: {FilePath}", removed, filePath);
        return removed;
    }

    /// <inheritdoc />
    public async Task<int> ReindexAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting full reindex of directory: {WatchPath}", _fileWatcherOptions.WatchPath);

        // Ensure vector store collection exists
        await _vectorStore.EnsureCollectionAsync(cancellationToken);

        // Get all supported files in the watch directory
        var files = GetAllSupportedFiles(_fileWatcherOptions.WatchPath);
        var processedCount = 0;

        foreach (var file in files)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                if (await ProcessFileAsync(file, cancellationToken))
                {
                    processedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file during reindex: {FilePath}", file);
            }
        }

        // Clean up orphaned documents
        await CleanupOrphanedDocumentsAsync(files, cancellationToken);

        _logger.LogInformation("Full reindex complete. Processed {Count} documents.", processedCount);
        return processedCount;
    }

    /// <summary>
    /// Generates a unique file ID based on the absolute path.
    /// </summary>
    public static string GenerateFileId(string filePath)
    {
        var absolutePath = Path.GetFullPath(filePath);
        var bytes = Encoding.UTF8.GetBytes(absolutePath.ToLowerInvariant());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Computes the SHA256 hash of a file's contents.
    /// </summary>
    private static async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private IEnumerable<string> GetAllSupportedFiles(string directory)
    {
        if (!Directory.Exists(directory))
            return [];

        var searchOption = _fileWatcherOptions.IncludeSubdirectories
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        var supportedExtensions = _fileWatcherOptions.SupportedExtensions
            .Select(e => e.ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Directory.EnumerateFiles(directory, "*.*", searchOption)
            .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
    }

    private async Task CleanupOrphanedDocumentsAsync(IEnumerable<string> currentFiles, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking for orphaned documents");

        var currentFileIds = currentFiles
            .Select(f => GenerateFileId(f))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var indexedDocuments = await _vectorStore.GetDocumentListAsync(cancellationToken);

        foreach (var doc in indexedDocuments)
        {
            if (!currentFileIds.Contains(doc.FileId))
            {
                _logger.LogInformation("Removing orphaned document: {FilePath}", doc.FilePath);
                await _vectorStore.DeleteByFileIdAsync(doc.FileId, cancellationToken);
            }
        }
    }
}
