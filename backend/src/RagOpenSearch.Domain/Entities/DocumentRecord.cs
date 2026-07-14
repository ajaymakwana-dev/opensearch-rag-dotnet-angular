namespace RagOpenSearch.Domain.Entities;

public sealed class DocumentRecord
{
    public string DocumentId { get; init; } = Guid.NewGuid().ToString("N");
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "text/plain";
    public DateTimeOffset IndexedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public int ChunkCount { get; set; }
}
