namespace RagOpenSearch.Application.DTOs;

public sealed record DocumentListItemDto(
    string DocumentId,
    string FileName,
    string ContentType,
    DateTimeOffset IndexedAtUtc,
    int ChunkCount);

public sealed record IngestDocumentResultDto(
    string DocumentId,
    string FileName,
    int ChunkCount,
    DateTimeOffset IndexedAtUtc);

public sealed record SearchHitDto(
    string ChunkId,
    string DocumentId,
    string FileName,
    int ChunkIndex,
    string Content,
    double Score);

public sealed record SearchRequestDto(string Query, int TopK = 5);

public sealed record SearchResponseDto(IReadOnlyList<SearchHitDto> Hits);

public sealed record AskRequestDto(string Question, int TopK = 5);

public sealed record AskResponseDto(
    string Answer,
    IReadOnlyList<SearchHitDto> Sources);
