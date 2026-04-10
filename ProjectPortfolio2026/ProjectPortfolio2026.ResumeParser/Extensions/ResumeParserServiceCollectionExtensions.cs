using Microsoft.Extensions.DependencyInjection;
using ProjectPortfolio2026.ResumeParser.Implementations;
using ProjectPortfolio2026.ResumeParser.Interfaces;

namespace ProjectPortfolio2026.ResumeParser.Extensions;

public static class ResumeParserServiceCollectionExtensions
{
    public static IServiceCollection AddResumeDocumentParser(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IResumeDocumentParser, HeuristicResumeDocumentParser>();
        return services;
    }
}
