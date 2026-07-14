namespace RagOpenSearch.Domain.Entities;

public sealed class DocumentChunk
{
    public string ChunkId { get; init; } = Guid.NewGuid().ToString("N");
    public string DocumentId { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public int ChunkIndex { get; init; }
    public string Content { get; init; } = string.Empty;
    public float[] Embedding { get; init; } = [];
    public DateTimeOffset IndexedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
