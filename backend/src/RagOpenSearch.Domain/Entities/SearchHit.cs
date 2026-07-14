namespace RagOpenSearch.Domain.Entities;

public sealed class SearchHit
{
    public string ChunkId { get; init; } = string.Empty;
    public string DocumentId { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public int ChunkIndex { get; init; }
    public string Content { get; init; } = string.Empty;
    public double Score { get; init; }
}
