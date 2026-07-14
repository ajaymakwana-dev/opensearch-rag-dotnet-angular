using RagOpenSearch.Application.DTOs;

namespace RagOpenSearch.Application.Interfaces;

public interface IDocumentIngestService
{
    Task<IngestDocumentResultDto> IngestTextAsync(
        string fileName,
        string content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentListItemDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string documentId, CancellationToken cancellationToken = default);
}
