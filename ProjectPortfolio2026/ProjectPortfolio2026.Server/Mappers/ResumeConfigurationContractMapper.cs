using ProjectPortfolio2026.Server.Contracts.Resume;
using ProjectPortfolio2026.Server.Domain.Portfolio;

namespace ProjectPortfolio2026.Server.Mappers;

public static class ResumeConfigurationContractMapper
{
    public static ResumeConfigurationResponse ToResponse(this ResumeConfiguration configuration, string? requestId = null)
    {
        return new ResumeConfigurationResponse
        {
            RequestId = requestId,
            Id = configuration.Id,
            SourceType = ResumeSourceTypes.Normalize(configuration.SourceType),
            SourceUrl = ResumeConfigurationRules.NormalizeText(configuration.SourceUrl),
            DisplayLabel = ResumeConfigurationRules.NormalizeText(configuration.DisplayLabel),
            Summary = ResumeConfigurationRules.NormalizeText(configuration.Summary),
            IsConfigured = ResumeConfigurationRules.HasCompletePublicConfiguration(configuration)
        };
    }
}
