using RagOpenSearch.Domain.Entities;

namespace RagOpenSearch.Domain.Services;

public interface ILlmService
{
    Task<string> GenerateAnswerAsync(
        string question,
        IReadOnlyList<SearchHit> evidence,
        CancellationToken cancellationToken = default);
}
