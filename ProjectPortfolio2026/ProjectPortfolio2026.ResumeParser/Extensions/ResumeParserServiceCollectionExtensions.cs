using Microsoft.Extensions.DependencyInjection;
using ProjectPortfolio2026.ResumeParser.Implementations;
using ProjectPortfolio2026.ResumeParser.Interfaces;

namespace ProjectPortfolio2026.ResumeParser.Extensions;

public static class ResumeParserServiceCollectionExtensions
{
    public static IServiceCollection AddResumeDocumentParser(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IResumeTextExtractor, DocxResumeTextExtractor>();
        services.AddScoped<IResumeTextExtractor, PdfResumeTextExtractor>();
        services.AddScoped<IResumeTextExtractor, PlainTextResumeTextExtractor>();
        services.AddScoped<IResumeSectionClassifier, HeuristicResumeSectionClassifier>();
        services.AddScoped<IResumeSectionParser, HeaderResumeSectionParser>();
        services.AddScoped<IResumeSectionParser, ProfessionalSummaryResumeSectionParser>();
        services.AddScoped<IResumeSectionParser, WorkExperienceResumeSectionParser>();
        services.AddScoped<IResumeSectionParser, EducationResumeSectionParser>();
        services.AddScoped<IResumeSectionParser, SkillsResumeSectionParser>();
        services.AddScoped<IResumeSectionParser, CertificationsResumeSectionParser>();
        services.AddScoped<IResumeSectionParser, ProjectsResumeSectionParser>();
        services.AddScoped<IResumeSectionParser, LanguagesResumeSectionParser>();
        services.AddScoped<IResumeSectionParser, AwardsResumeSectionParser>();
        services.AddScoped<IResumeSectionParser, VolunteerExperienceResumeSectionParser>();
        services.AddScoped<IResumeSectionParser, PublicationsResumeSectionParser>();
        services.AddScoped<IResumeSectionParser, ReferencesResumeSectionParser>();
        services.AddScoped<IResumeAdditionalSectionParser, AdditionalResumeSectionParser>();
        services.AddScoped<IResumeDocumentParser, HeuristicResumeDocumentParser>();
        return services;
    }
}
