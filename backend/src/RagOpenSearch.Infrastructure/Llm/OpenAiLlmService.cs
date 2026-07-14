using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RagOpenSearch.Domain.Entities;
using RagOpenSearch.Domain.Services;
using RagOpenSearch.Infrastructure.Options;

namespace RagOpenSearch.Infrastructure.Llm;

public sealed class OpenAiLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly LlmOptions _options;

    public OpenAiLlmService(HttpClient httpClient, IOptions<LlmOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
    }

    public async Task<string> GenerateAnswerAsync(
        string question,
        IReadOnlyList<SearchHit> evidence,
        CancellationToken cancellationToken = default)
    {
        var context = new StringBuilder();
        for (var i = 0; i < evidence.Count; i++)
        {
            var hit = evidence[i];
            context.AppendLine($"[Source {i + 1}] {hit.FileName} (chunk {hit.ChunkIndex})");
            context.AppendLine(hit.Content);
            context.AppendLine();
        }

        var payload = new
        {
            model = _options.ChatModel,
            temperature = 0.2,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "You are a helpful enterprise assistant. Answer only using the provided sources. "
                              + "If the sources do not contain the answer, say you do not know. Cite sources as [Source N]."
                },
                new
                {
                    role = "user",
                    content = $"Sources:\n{context}\nQuestion: {question}"
                }
            }
        };

        using var response = await _httpClient.PostAsJsonAsync("chat/completions", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var parsed = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: cancellationToken)
                     ?? throw new InvalidOperationException("Empty chat response.");

        return parsed.Choices.FirstOrDefault()?.Message?.Content?.Trim()
               ?? "No answer generated.";
    }

    private sealed class ChatResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; } = [];
    }

    private sealed class Choice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; set; }
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
