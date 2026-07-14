using RagOpenSearch.Application.DTOs;
using RagOpenSearch.Application.Interfaces;
using RagOpenSearch.Domain.Entities;
using RagOpenSearch.Domain.Repositories;
using RagOpenSearch.Domain.Services;

namespace RagOpenSearch.Application.Services;

public sealed class DocumentIngestService : IDocumentIngestService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly ITextChunker _textChunker;

    public DocumentIngestService(
        IDocumentRepository documentRepository,
        IEmbeddingService embeddingService,
        ITextChunker textChunker)
    {
        _documentRepository = documentRepository;
        _embeddingService = embeddingService;
        _textChunker = textChunker;
    }

    public async Task<IngestDocumentResultDto> IngestTextAsync(
        string fileName,
        string content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Document content is empty.", nameof(content));
        }

        await _documentRepository.EnsureIndicesAsync(cancellationToken);

        var document = new DocumentRecord
        {
            FileName = string.IsNullOrWhiteSpace(fileName) ? "untitled.txt" : fileName.Trim(),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "text/plain" : contentType
        };

        var pieces = _textChunker.Chunk(content);
        var embeddings = await _embeddingService.EmbedBatchAsync(pieces, cancellationToken);

        var chunks = pieces
            .Select((text, index) => new DocumentChunk
            {
                DocumentId = document.DocumentId,
                FileName = document.FileName,
                ChunkIndex = index,
                Content = text,
                Embedding = embeddings[index]
            })
            .ToList();

        document.ChunkCount = chunks.Count;
        await _documentRepository.IndexDocumentAsync(document, chunks, cancellationToken);

        return new IngestDocumentResultDto(
            document.DocumentId,
            document.FileName,
            document.ChunkCount,
            document.IndexedAtUtc);
    }

    public async Task<IReadOnlyList<DocumentListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        await _documentRepository.EnsureIndicesAsync(cancellationToken);
        var docs = await _documentRepository.ListDocumentsAsync(cancellationToken);
        return docs
            .Select(d => new DocumentListItemDto(
                d.DocumentId,
                d.FileName,
                d.ContentType,
                d.IndexedAtUtc,
                d.ChunkCount))
            .ToList();
    }

    public Task<bool> DeleteAsync(string documentId, CancellationToken cancellationToken = default)
        => _documentRepository.DeleteDocumentAsync(documentId, cancellationToken);
}
