using RagOpenSearch.Domain.Entities;

namespace RagOpenSearch.Domain.Repositories;

public interface IDocumentRepository
{
    Task EnsureIndicesAsync(CancellationToken cancellationToken = default);
    Task IndexDocumentAsync(DocumentRecord document, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentRecord>> ListDocumentsAsync(CancellationToken cancellationToken = default);
    Task<bool> DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default);
}
