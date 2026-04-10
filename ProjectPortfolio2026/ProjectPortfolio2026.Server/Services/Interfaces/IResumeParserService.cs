using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Services.Interfaces;

public interface IResumeParserService
{
    Task<ResumeParseResult> ParseAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default);
}
