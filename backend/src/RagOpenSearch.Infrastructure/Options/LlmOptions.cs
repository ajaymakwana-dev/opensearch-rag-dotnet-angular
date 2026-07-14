namespace RagOpenSearch.Infrastructure.Options;

public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>Use "OpenAI" or "Mock".</summary>
    public string Provider { get; set; } = "Mock";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public string ChatModel { get; set; } = "gpt-4o-mini";
    public int EmbeddingDimensions { get; set; } = 384;
}
