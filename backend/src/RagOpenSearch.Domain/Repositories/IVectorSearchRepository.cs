using RagOpenSearch.Domain.Entities;

namespace RagOpenSearch.Domain.Repositories;

public interface IVectorSearchRepository
{
    Task<IReadOnlyList<SearchHit>> SemanticSearchAsync(
        float[] queryEmbedding,
        int topK,
        CancellationToken cancellationToken = default);
}
