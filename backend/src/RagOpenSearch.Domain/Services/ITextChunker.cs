namespace RagOpenSearch.Domain.Services;

public interface ITextChunker
{
    IReadOnlyList<string> Chunk(string text, int chunkSize = 800, int overlap = 120);
}
