using RagOpenSearch.Application.DTOs;
using RagOpenSearch.Application.Interfaces;
using RagOpenSearch.Domain.Repositories;
using RagOpenSearch.Domain.Services;

namespace RagOpenSearch.Application.Services;

public sealed class RagQueryService : IRagQueryService
{
    private readonly IVectorSearchRepository _vectorSearchRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILlmService _llmService;
    private readonly IDocumentRepository _documentRepository;

    public RagQueryService(
        IVectorSearchRepository vectorSearchRepository,
        IEmbeddingService embeddingService,
        ILlmService llmService,
        IDocumentRepository documentRepository)
    {
        _vectorSearchRepository = vectorSearchRepository;
        _embeddingService = embeddingService;
        _llmService = llmService;
        _documentRepository = documentRepository;
    }

    public async Task<SearchResponseDto> SearchAsync(SearchRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            throw new ArgumentException("Query is required.", nameof(request));
        }

        await _documentRepository.EnsureIndicesAsync(cancellationToken);

        var embedding = await _embeddingService.EmbedAsync(request.Query, cancellationToken);
        var topK = Math.Clamp(request.TopK, 1, 20);
        var hits = await _vectorSearchRepository.SemanticSearchAsync(embedding, topK, cancellationToken);

        return new SearchResponseDto(hits.Select(MapHit).ToList());
    }

    public async Task<AskResponseDto> AskAsync(AskRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            throw new ArgumentException("Question is required.", nameof(request));
        }

        await _documentRepository.EnsureIndicesAsync(cancellationToken);

        var embedding = await _embeddingService.EmbedAsync(request.Question, cancellationToken);
        var topK = Math.Clamp(request.TopK, 1, 20);
        var hits = await _vectorSearchRepository.SemanticSearchAsync(embedding, topK, cancellationToken);
        var answer = await _llmService.GenerateAnswerAsync(request.Question, hits, cancellationToken);

        return new AskResponseDto(answer, hits.Select(MapHit).ToList());
    }

    private static SearchHitDto MapHit(Domain.Entities.SearchHit hit)
        => new(hit.ChunkId, hit.DocumentId, hit.FileName, hit.ChunkIndex, hit.Content, hit.Score);
}
