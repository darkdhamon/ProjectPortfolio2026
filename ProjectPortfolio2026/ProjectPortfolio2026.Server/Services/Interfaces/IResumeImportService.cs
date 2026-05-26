using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Services.Interfaces;

public interface IResumeImportService
{
    Task<ResumeImportParseResult> ParseAsync(IFormFile file, CancellationToken cancellationToken = default);
}
