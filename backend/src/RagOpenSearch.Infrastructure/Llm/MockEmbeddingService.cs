using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using RagOpenSearch.Domain.Services;
using RagOpenSearch.Infrastructure.Options;

namespace RagOpenSearch.Infrastructure.Llm;

/// <summary>
/// Deterministic local embeddings for demo runs without API keys.
/// Vectors are stable per input text so semantic-ish matching still works within the demo corpus.
/// </summary>
public sealed class MockEmbeddingService : IEmbeddingService
{
    private readonly int _dimensions;

    public MockEmbeddingService(IOptions<LlmOptions> options)
    {
        _dimensions = Math.Clamp(options.Value.EmbeddingDimensions, 32, 1536);
    }

    public int Dimensions => _dimensions;

    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
        => Task.FromResult(CreateEmbedding(text));

    public Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<float[]>>(texts.Select(CreateEmbedding).ToList());

    private float[] CreateEmbedding(string text)
    {
        var vector = new float[_dimensions];
        var tokens = Tokenize(text);
        if (tokens.Count == 0)
        {
            vector[0] = 1f;
            return vector;
        }

        foreach (var token in tokens)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            var index = BitConverter.ToUInt16(hash, 0) % _dimensions;
            var sign = (hash[2] & 1) == 0 ? 1f : -1f;
            vector[index] += sign;
        }

        // lightly boost overlapping substrings of length 3 for better demo retrieval
        for (var i = 0; i < text.Length - 2; i++)
        {
            var gram = text.Substring(i, 3).ToLowerInvariant();
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(gram));
            var index = BitConverter.ToUInt16(hash, 0) % _dimensions;
            vector[index] += 0.25f;
        }

        Normalize(vector);
        return vector;
    }

    private static List<string> Tokenize(string text)
        => text
            .ToLowerInvariant()
            .Split([' ', '\n', '\t', ',', '.', ';', ':', '!', '?', '/', '\\', '(', ')', '[', ']', '"', '\''],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    private static void Normalize(float[] vector)
    {
        double sum = 0;
        foreach (var v in vector)
        {
            sum += v * v;
        }

        var norm = Math.Sqrt(sum);
        if (norm < 1e-9)
        {
            return;
        }

        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] = (float)(vector[i] / norm);
        }
    }
}
