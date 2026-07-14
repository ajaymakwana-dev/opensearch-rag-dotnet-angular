using Microsoft.Extensions.Options;
using OpenSearch.Client;
using OpenSearch.Net;
using RagOpenSearch.Domain.Entities;
using RagOpenSearch.Domain.Repositories;
using RagOpenSearch.Infrastructure.Options;

namespace RagOpenSearch.Infrastructure.OpenSearch;

public sealed class OpenSearchVectorSearchRepository : IVectorSearchRepository
{
    private readonly IOpenSearchClient _client;
    private readonly OpenSearchOptions _options;

    public OpenSearchVectorSearchRepository(IOpenSearchClient client, IOptions<OpenSearchOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<SearchHit>> SemanticSearchAsync(
        float[] queryEmbedding,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var query = new
        {
            size = topK,
            query = new
            {
                knn = new
                {
                    embedding = new
                    {
                        vector = queryEmbedding,
                        k = topK
                    }
                }
            }
        };

        var response = await _client.LowLevel.SearchAsync<SearchResponse<ChunkSource>>(
            _options.ChunksIndex,
            PostData.Serializable(query),
            ctx: cancellationToken);

        if (!response.IsValid)
        {
            throw new InvalidOperationException($"OpenSearch k-NN search failed: {response.DebugInformation}");
        }

        return response.Hits
            .Select(h => new SearchHit
            {
                ChunkId = h.Source.ChunkId,
                DocumentId = h.Source.DocumentId,
                FileName = h.Source.FileName,
                ChunkIndex = h.Source.ChunkIndex,
                Content = h.Source.Content,
                Score = h.Score ?? 0
            })
            .ToList();
    }

    private sealed class ChunkSource
    {
        public string ChunkId { get; set; } = string.Empty;
        public string DocumentId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
