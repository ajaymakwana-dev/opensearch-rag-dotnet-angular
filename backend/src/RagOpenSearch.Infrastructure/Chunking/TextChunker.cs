using RagOpenSearch.Domain.Services;

namespace RagOpenSearch.Infrastructure.Chunking;

public sealed class TextChunker : ITextChunker
{
    public IReadOnlyList<string> Chunk(string text, int chunkSize = 800, int overlap = 120)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var normalized = text.Replace("\r\n", "\n").Trim();
        if (normalized.Length <= chunkSize)
        {
            return [normalized];
        }

        var chunks = new List<string>();
        var step = Math.Max(1, chunkSize - overlap);
        for (var start = 0; start < normalized.Length; start += step)
        {
            var length = Math.Min(chunkSize, normalized.Length - start);
            chunks.Add(normalized.Substring(start, length).Trim());
            if (start + length >= normalized.Length)
            {
                break;
            }
        }

        return chunks.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
    }
}
