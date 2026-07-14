using Microsoft.Extensions.DependencyInjection;
using RagOpenSearch.Application.Interfaces;
using RagOpenSearch.Application.Services;

namespace RagOpenSearch.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDocumentIngestService, DocumentIngestService>();
        services.AddScoped<IRagQueryService, RagQueryService>();
        return services;
    }
}
