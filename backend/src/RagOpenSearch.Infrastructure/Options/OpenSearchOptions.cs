namespace RagOpenSearch.Infrastructure.Options;

public sealed class OpenSearchOptions
{
    public const string SectionName = "OpenSearch";

    public string Uri { get; set; } = "http://localhost:9200";
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "Admin@123456!";
    public bool SkipCertificateValidation { get; set; } = true;
    public string DocumentsIndex { get; set; } = "rag-documents";
    public string ChunksIndex { get; set; } = "rag-chunks";
}
