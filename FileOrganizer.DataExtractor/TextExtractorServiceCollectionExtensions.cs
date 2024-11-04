using FileOrganizer.DataExtractor.DocumentConnectors;
using Microsoft.Extensions.DependencyInjection;

namespace FileOrganizer.DataExtractor;

public static class TextExtractorServiceCollectionExtensions
{
    public static IServiceCollection AddTextExtraction(this IServiceCollection services)
    {
        services.AddScoped<ITextExtractorService>(_ => new TextExtractorService(string.Empty));
        services.AddScoped<TextExtractorFactory>();
        services.AddScoped<IDocumentTextExtractor, DocumentTextExtractor>();

        return services;
    }
}