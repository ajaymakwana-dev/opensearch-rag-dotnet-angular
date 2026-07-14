using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenSearch.Client;
using OpenSearch.Net;
using RagOpenSearch.Domain.Entities;
using RagOpenSearch.Domain.Repositories;
using RagOpenSearch.Domain.Services;
using RagOpenSearch.Infrastructure.Options;

namespace RagOpenSearch.Infrastructure.OpenSearch;

public sealed class OpenSearchDocumentRepository : IDocumentRepository
{
    private readonly IOpenSearchClient _client;
    private readonly OpenSearchOptions _options;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<OpenSearchDocumentRepository> _logger;

    public OpenSearchDocumentRepository(
        IOpenSearchClient client,
        IOptions<OpenSearchOptions> options,
        IEmbeddingService embeddingService,
        ILogger<OpenSearchDocumentRepository> logger)
    {
        _client = client;
        _options = options.Value;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task EnsureIndicesAsync(CancellationToken cancellationToken = default)
    {
        var docsExists = await _client.Indices.ExistsAsync(_options.DocumentsIndex, ct: cancellationToken);
        if (!docsExists.Exists)
        {
            var createDocs = await _client.Indices.CreateAsync(_options.DocumentsIndex, c => c
                .Map<DocumentMetadataSource>(m => m
                    .Properties(p => p
                        .Keyword(k => k.Name(n => n.DocumentId))
                        .Keyword(k => k.Name(n => n.FileName))
                        .Keyword(k => k.Name(n => n.ContentType))
                        .Date(d => d.Name(n => n.IndexedAtUtc))
                        .Number(n => n.Name(x => x.ChunkCount).Type(NumberType.Integer))
                    )
                ), cancellationToken);

            if (!createDocs.IsValid)
            {
                throw new InvalidOperationException($"Failed to create documents index: {createDocs.DebugInformation}");
            }
        }

        var chunksExists = await _client.Indices.ExistsAsync(_options.ChunksIndex, ct: cancellationToken);
        if (!chunksExists.Exists)
        {
            var dims = _embeddingService.Dimensions;
            var body = new
            {
                settings = new
                {
                    index = new
                    {
                        knn = true,
                        number_of_shards = 1,
                        number_of_replicas = 0
                    }
                },
                mappings = new
                {
                    properties = new Dictionary<string, object>
                    {
                        ["chunkId"] = new { type = "keyword" },
                        ["documentId"] = new { type = "keyword" },
                        ["fileName"] = new { type = "keyword" },
                        ["chunkIndex"] = new { type = "integer" },
                        ["content"] = new { type = "text" },
                        ["indexedAtUtc"] = new { type = "date" },
                        ["embedding"] = new
                        {
                            type = "knn_vector",
                            dimension = dims,
                            method = new
                            {
                                name = "hnsw",
                                space_type = "cosinesimil",
                                engine = "nmslib"
                            }
                        }
                    }
                }
            };

            var createChunks = await _client.LowLevel.Indices.CreateAsync<StringResponse>(
                _options.ChunksIndex,
                PostData.Serializable(body),
                ctx: cancellationToken);

            if (!createChunks.Success)
            {
                throw new InvalidOperationException($"Failed to create chunks index: {createChunks.Body}");
            }

            _logger.LogInformation("Created OpenSearch chunks index {Index} with {Dims} dimensions.",
                _options.ChunksIndex, dims);
        }
    }

    public async Task IndexDocumentAsync(
        DocumentRecord document,
        IReadOnlyList<DocumentChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        var meta = new DocumentMetadataSource
        {
            DocumentId = document.DocumentId,
            FileName = document.FileName,
            ContentType = document.ContentType,
            IndexedAtUtc = document.IndexedAtUtc,
            ChunkCount = document.ChunkCount
        };

        var metaResponse = await _client.IndexAsync(meta, i => i
            .Index(_options.DocumentsIndex)
            .Id(document.DocumentId)
            .Refresh(global::OpenSearch.Net.Refresh.True), cancellationToken);

        if (!metaResponse.IsValid)
        {
            throw new InvalidOperationException($"Failed to index document metadata: {metaResponse.DebugInformation}");
        }

        var descriptor = new BulkDescriptor();
        foreach (var chunk in chunks)
        {
            var source = new ChunkSource
            {
                ChunkId = chunk.ChunkId,
                DocumentId = chunk.DocumentId,
                FileName = chunk.FileName,
                ChunkIndex = chunk.ChunkIndex,
                Content = chunk.Content,
                IndexedAtUtc = chunk.IndexedAtUtc,
                Embedding = chunk.Embedding
            };

            descriptor.Index<ChunkSource>(op => op
                .Index(_options.ChunksIndex)
                .Id(chunk.ChunkId)
                .Document(source));
        }

        var bulk = await _client.BulkAsync(descriptor.Refresh(global::OpenSearch.Net.Refresh.True), cancellationToken);
        if (!bulk.IsValid || bulk.Errors)
        {
            throw new InvalidOperationException($"Failed to bulk index chunks: {bulk.DebugInformation}");
        }
    }

    public async Task<IReadOnlyList<DocumentRecord>> ListDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _client.SearchAsync<DocumentMetadataSource>(s => s
            .Index(_options.DocumentsIndex)
            .Size(200)
            .Sort(ss => ss.Descending(f => f.IndexedAtUtc))
            .Query(q => q.MatchAll()), cancellationToken);

        if (!response.IsValid)
        {
            throw new InvalidOperationException($"Failed to list documents: {response.DebugInformation}");
        }

        return response.Documents.Select(d => new DocumentRecord
        {
            DocumentId = d.DocumentId,
            FileName = d.FileName,
            ContentType = d.ContentType,
            IndexedAtUtc = d.IndexedAtUtc,
            ChunkCount = d.ChunkCount
        }).ToList();
    }

    public async Task<bool> DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        var deleteMeta = await _client.DeleteAsync<DocumentMetadataSource>(
            documentId,
            d => d.Index(_options.DocumentsIndex).Refresh(global::OpenSearch.Net.Refresh.True),
            cancellationToken);

        var deleteChunks = await _client.DeleteByQueryAsync<ChunkSource>(d => d
            .Index(_options.ChunksIndex)
            .Query(q => q.Term(t => t.Field(f => f.DocumentId).Value(documentId)))
            .Refresh(true), cancellationToken);

        return deleteMeta.IsValid || (deleteChunks.IsValid && deleteChunks.Deleted > 0);
    }

    private sealed class DocumentMetadataSource
    {
        public string DocumentId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public DateTimeOffset IndexedAtUtc { get; set; }
        public int ChunkCount { get; set; }
    }

    private sealed class ChunkSource
    {
        public string ChunkId { get; set; } = string.Empty;
        public string DocumentId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTimeOffset IndexedAtUtc { get; set; }
        public float[] Embedding { get; set; } = [];
    }
}
