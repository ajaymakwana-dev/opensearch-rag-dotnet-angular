using RagOpenSearch.Application.DTOs;

namespace RagOpenSearch.Application.Interfaces;

public interface IRagQueryService
{
    Task<SearchResponseDto> SearchAsync(SearchRequestDto request, CancellationToken cancellationToken = default);
    Task<AskResponseDto> AskAsync(AskRequestDto request, CancellationToken cancellationToken = default);
}
