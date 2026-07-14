using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenSearch.Client;
using OpenSearch.Net;
using RagOpenSearch.Domain.Repositories;
using RagOpenSearch.Domain.Services;
using RagOpenSearch.Infrastructure.Chunking;
using RagOpenSearch.Infrastructure.Llm;
using RagOpenSearch.Infrastructure.OpenSearch;
using RagOpenSearch.Infrastructure.Options;

namespace RagOpenSearch.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenSearchOptions>(configuration.GetSection(OpenSearchOptions.SectionName));
        services.Configure<LlmOptions>(configuration.GetSection(LlmOptions.SectionName));

        services.AddSingleton<IOpenSearchClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OpenSearchOptions>>().Value;
            var uri = new Uri(options.Uri);
            var settings = new ConnectionSettings(uri)
                .DefaultFieldNameInferrer(p => char.ToLowerInvariant(p[0]) + p[1..])
                .DisableDirectStreaming()
                .PrettyJson();

            if (!string.IsNullOrWhiteSpace(options.Username))
            {
                settings = settings.BasicAuthentication(options.Username, options.Password);
            }

            if (options.SkipCertificateValidation)
            {
                settings = settings.ServerCertificateValidationCallback(CertificateValidations.AllowAll);
            }

            return new OpenSearchClient(settings);
        });

        services.AddSingleton<ITextChunker, TextChunker>();
        services.AddScoped<IDocumentRepository, OpenSearchDocumentRepository>();
        services.AddScoped<IVectorSearchRepository, OpenSearchVectorSearchRepository>();

        var provider = configuration.GetSection(LlmOptions.SectionName).GetValue<string>("Provider") ?? "Mock";
        if (string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IEmbeddingService, OpenAiEmbeddingService>();
            services.AddHttpClient<ILlmService, OpenAiLlmService>();
        }
        else
        {
            services.AddSingleton<IEmbeddingService, MockEmbeddingService>();
            services.AddSingleton<ILlmService, MockLlmService>();
        }

        return services;
    }
}
