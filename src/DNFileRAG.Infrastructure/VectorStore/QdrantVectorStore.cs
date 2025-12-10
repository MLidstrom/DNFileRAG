using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DNFileRAG.Core.Configuration;
using DNFileRAG.Core.Interfaces;
using DNFileRAG.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DNFileRAG.Infrastructure.VectorStore;

/// <summary>
/// Vector store implementation using Qdrant REST API.
/// </summary>
public class QdrantVectorStore : IVectorStore
{
    private readonly HttpClient _httpClient;
    private readonly QdrantOptions _options;
    private readonly ILogger<QdrantVectorStore> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Payload field names
    private const string FieldFileId = "file_id";
    private const string FieldFilePath = "file_path";
    private const string FieldFileName = "file_name";
    private const string FieldFileHash = "file_hash";
    private const string FieldChunkIndex = "chunk_index";
    private const string FieldPageNumber = "page_number";
    private const string FieldContent = "content";
    private const string FieldCreatedAt = "created_at";
    private const string FieldUpdatedAt = "updated_at";
    private const string FieldIsActive = "is_active";

    public QdrantVectorStore(
        HttpClient httpClient,
        IOptions<QdrantOptions> options,
        ILogger<QdrantVectorStore> logger)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri($"http://{_options.Host}:{_options.Port}");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc />
    public async Task EnsureCollectionAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"/collections/{_options.CollectionName}",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Creating collection {CollectionName} with vector size {VectorSize}",
                _options.CollectionName, _options.VectorSize);

            var createRequest = new
            {
                vectors = new
                {
                    size = _options.VectorSize,
                    distance = "Cosine"
                }
            };

            var createResponse = await _httpClient.PutAsJsonAsync(
                $"/collections/{_options.CollectionName}",
                createRequest,
                _jsonOptions,
                cancellationToken);
            createResponse.EnsureSuccessStatusCode();

            // Create indexes for common filter fields
            await CreatePayloadIndexAsync(FieldFileId, "keyword", cancellationToken);
            await CreatePayloadIndexAsync(FieldIsActive, "bool", cancellationToken);

            _logger.LogInformation("Collection {CollectionName} created successfully", _options.CollectionName);
        }
        else
        {
            response.EnsureSuccessStatusCode();
            _logger.LogDebug("Collection {CollectionName} already exists", _options.CollectionName);
        }
    }

    private async Task CreatePayloadIndexAsync(string fieldName, string fieldType, CancellationToken cancellationToken)
    {
        var request = new
        {
            field_name = fieldName,
            field_schema = fieldType
        };

        var response = await _httpClient.PutAsJsonAsync(
            $"/collections/{_options.CollectionName}/index",
            request,
            _jsonOptions,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task UpsertChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        var chunkList = chunks.ToList();
        if (chunkList.Count == 0)
            return;

        _logger.LogDebug("Upserting {Count} chunks to collection {CollectionName}",
            chunkList.Count, _options.CollectionName);

        var points = chunkList.Select(chunk =>
        {
            var payload = new Dictionary<string, object>
            {
                [FieldFileId] = chunk.Metadata.FileId,
                [FieldFilePath] = chunk.Metadata.FilePath,
                [FieldFileName] = chunk.Metadata.FileName,
                [FieldFileHash] = chunk.Metadata.FileHash,
                [FieldChunkIndex] = chunk.Metadata.ChunkIndex,
                [FieldContent] = chunk.Content,
                [FieldCreatedAt] = chunk.Metadata.CreatedAt.ToString("O"),
                [FieldUpdatedAt] = chunk.Metadata.UpdatedAt.ToString("O"),
                [FieldIsActive] = chunk.Metadata.IsActive
            };

            if (chunk.Metadata.PageNumber.HasValue)
            {
                payload[FieldPageNumber] = chunk.Metadata.PageNumber.Value;
            }

            return new
            {
                id = chunk.Id,
                vector = chunk.Embedding,
                payload
            };
        }).ToList();

        var request = new { points };

        var response = await _httpClient.PutAsJsonAsync(
            $"/collections/{_options.CollectionName}/points",
            request,
            _jsonOptions,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        _logger.LogDebug("Successfully upserted {Count} chunks", chunkList.Count);
    }

    /// <inheritdoc />
    public async Task<int> DeleteByFileIdAsync(string fileId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileId);

        _logger.LogDebug("Deleting chunks for file_id {FileId}", fileId);

        // First count
        var countRequest = new
        {
            filter = new
            {
                must = new[]
                {
                    new
                    {
                        key = FieldFileId,
                        match = new { value = fileId }
                    }
                }
            },
            exact = true
        };

        var countResponse = await _httpClient.PostAsJsonAsync(
            $"/collections/{_options.CollectionName}/points/count",
            countRequest,
            _jsonOptions,
            cancellationToken);
        countResponse.EnsureSuccessStatusCode();

        var countResult = await countResponse.Content.ReadFromJsonAsync<QdrantCountResponse>(_jsonOptions, cancellationToken);
        var count = countResult?.Result?.Count ?? 0;

        if (count > 0)
        {
            var deleteRequest = new
            {
                filter = new
                {
                    must = new[]
                    {
                        new
                        {
                            key = FieldFileId,
                            match = new { value = fileId }
                        }
                    }
                }
            };

            var deleteResponse = await _httpClient.PostAsJsonAsync(
                $"/collections/{_options.CollectionName}/points/delete",
                deleteRequest,
                _jsonOptions,
                cancellationToken);
            deleteResponse.EnsureSuccessStatusCode();

            _logger.LogInformation("Deleted {Count} chunks for file_id {FileId}", count, fileId);
        }

        return (int)count;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        float[] queryVector,
        int topK,
        SearchFilters? filters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryVector);

        _logger.LogDebug("Searching for {TopK} similar chunks", topK);

        var searchRequest = new Dictionary<string, object>
        {
            ["vector"] = queryVector,
            ["limit"] = topK,
            ["with_payload"] = true
        };

        var filter = BuildFilter(filters);
        if (filter != null)
        {
            searchRequest["filter"] = filter;
        }

        var response = await _httpClient.PostAsJsonAsync(
            $"/collections/{_options.CollectionName}/points/search",
            searchRequest,
            _jsonOptions,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<QdrantSearchResponse>(_jsonOptions, cancellationToken);

        return result?.Result?.Select(r => new SearchResult
        {
            Score = r.Score,
            Content = r.Payload?.GetValueOrDefault(FieldContent)?.ToString() ?? string.Empty,
            Metadata = new ChunkMetadata
            {
                FileId = r.Payload?.GetValueOrDefault(FieldFileId)?.ToString() ?? string.Empty,
                FilePath = r.Payload?.GetValueOrDefault(FieldFilePath)?.ToString() ?? string.Empty,
                FileName = r.Payload?.GetValueOrDefault(FieldFileName)?.ToString() ?? string.Empty,
                FileHash = r.Payload?.GetValueOrDefault(FieldFileHash)?.ToString() ?? string.Empty,
                ChunkIndex = GetIntValue(r.Payload, FieldChunkIndex),
                PageNumber = GetNullableIntValue(r.Payload, FieldPageNumber),
                CreatedAt = DateTime.Parse(r.Payload?.GetValueOrDefault(FieldCreatedAt)?.ToString() ?? DateTime.UtcNow.ToString("O")),
                UpdatedAt = DateTime.Parse(r.Payload?.GetValueOrDefault(FieldUpdatedAt)?.ToString() ?? DateTime.UtcNow.ToString("O")),
                IsActive = GetBoolValue(r.Payload, FieldIsActive)
            }
        }).ToList() ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentInfo>> GetDocumentListAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting document list from collection {CollectionName}", _options.CollectionName);

        var documents = new Dictionary<string, (string FilePath, string FileName, DateTime LastIndexed, int Count)>();
        string? offset = null;
        const int limit = 100;

        while (true)
        {
            var scrollRequest = new Dictionary<string, object>
            {
                ["limit"] = limit,
                ["with_payload"] = new[] { FieldFileId, FieldFilePath, FieldFileName, FieldUpdatedAt }
            };

            if (offset != null)
            {
                scrollRequest["offset"] = offset;
            }

            var response = await _httpClient.PostAsJsonAsync(
                $"/collections/{_options.CollectionName}/points/scroll",
                scrollRequest,
                _jsonOptions,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<QdrantScrollResponse>(_jsonOptions, cancellationToken);
            var points = result?.Result?.Points ?? [];

            if (points.Count == 0)
                break;

            foreach (var point in points)
            {
                var fileId = point.Payload?.GetValueOrDefault(FieldFileId)?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(fileId))
                    continue;

                var filePath = point.Payload?.GetValueOrDefault(FieldFilePath)?.ToString() ?? string.Empty;
                var fileName = point.Payload?.GetValueOrDefault(FieldFileName)?.ToString() ?? string.Empty;
                var updatedStr = point.Payload?.GetValueOrDefault(FieldUpdatedAt)?.ToString();
                var updated = !string.IsNullOrEmpty(updatedStr) ? DateTime.Parse(updatedStr) : DateTime.UtcNow;

                if (!documents.ContainsKey(fileId))
                {
                    documents[fileId] = (filePath, fileName, updated, 1);
                }
                else
                {
                    var (existingPath, existingName, lastIndexed, count) = documents[fileId];
                    documents[fileId] = (existingPath, existingName, updated > lastIndexed ? updated : lastIndexed, count + 1);
                }
            }

            offset = result?.Result?.NextPageOffset;
            if (offset == null)
                break;
        }

        return documents
            .Select(kvp => new DocumentInfo
            {
                FileId = kvp.Key,
                FilePath = kvp.Value.FilePath,
                FileName = kvp.Value.FileName,
                LastIndexed = kvp.Value.LastIndexed,
                ChunkCount = kvp.Value.Count
            })
            .OrderBy(d => d.FileName)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<bool> IsDocumentIndexedAsync(string fileId, string fileHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileId);
        ArgumentNullException.ThrowIfNull(fileHash);

        var countRequest = new
        {
            filter = new
            {
                must = new object[]
                {
                    new { key = FieldFileId, match = new { value = fileId } },
                    new { key = FieldFileHash, match = new { value = fileHash } }
                }
            },
            exact = true
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/collections/{_options.CollectionName}/points/count",
            countRequest,
            _jsonOptions,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<QdrantCountResponse>(_jsonOptions, cancellationToken);
        return (result?.Result?.Count ?? 0) > 0;
    }

    private static object? BuildFilter(SearchFilters? filters)
    {
        if (filters == null)
            return null;

        var conditions = new List<object>();

        // Always filter for active chunks by default
        if (filters.IsActive != false)
        {
            conditions.Add(new { key = FieldIsActive, match = new { value = true } });
        }

        // Filter by file paths (any match)
        if (filters.FilePaths is { Length: > 0 })
        {
            if (filters.FilePaths.Length == 1)
            {
                conditions.Add(new { key = FieldFilePath, match = new { text = filters.FilePaths[0] } });
            }
            else
            {
                var pathConditions = filters.FilePaths
                    .Select(path => new { key = FieldFilePath, match = new { text = path } })
                    .ToArray();
                conditions.Add(new { should = pathConditions });
            }
        }

        if (conditions.Count == 0)
            return null;

        return new { must = conditions };
    }

    private static int GetIntValue(Dictionary<string, object>? payload, string key)
    {
        if (payload == null || !payload.TryGetValue(key, out var value))
            return 0;

        return value switch
        {
            JsonElement je => je.ValueKind == JsonValueKind.Number ? je.GetInt32() : 0,
            int i => i,
            long l => (int)l,
            _ => int.TryParse(value.ToString(), out var result) ? result : 0
        };
    }

    private static int? GetNullableIntValue(Dictionary<string, object>? payload, string key)
    {
        if (payload == null || !payload.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            JsonElement je when je.ValueKind == JsonValueKind.Number => je.GetInt32(),
            JsonElement je when je.ValueKind == JsonValueKind.Null => null,
            int i => i,
            long l => (int)l,
            null => null,
            _ => int.TryParse(value.ToString(), out var result) ? result : null
        };
    }

    private static bool GetBoolValue(Dictionary<string, object>? payload, string key)
    {
        if (payload == null || !payload.TryGetValue(key, out var value))
            return false;

        return value switch
        {
            JsonElement je => je.ValueKind == JsonValueKind.True,
            bool b => b,
            _ => bool.TryParse(value.ToString(), out var result) && result
        };
    }

    // Response DTOs
    private class QdrantCountResponse
    {
        public QdrantCountResult? Result { get; set; }
    }

    private class QdrantCountResult
    {
        public long Count { get; set; }
    }

    private class QdrantSearchResponse
    {
        public List<QdrantSearchResult>? Result { get; set; }
    }

    private class QdrantSearchResult
    {
        public float Score { get; set; }
        public Dictionary<string, object>? Payload { get; set; }
    }

    private class QdrantScrollResponse
    {
        public QdrantScrollResult? Result { get; set; }
    }

    private class QdrantScrollResult
    {
        public List<QdrantScrollPoint>? Points { get; set; }
        [JsonPropertyName("next_page_offset")]
        public string? NextPageOffset { get; set; }
    }

    private class QdrantScrollPoint
    {
        public Dictionary<string, object>? Payload { get; set; }
    }
}
