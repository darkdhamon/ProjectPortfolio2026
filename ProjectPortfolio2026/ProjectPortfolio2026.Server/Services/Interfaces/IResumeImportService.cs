using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Services.Interfaces;

public interface IResumeImportService
{
    Task<ResumeImportCandidateResult> ParseAsync(IFormFile file, CancellationToken cancellationToken = default);

    Task<ResumeImportCandidateResult> ParseConfiguredSourceAsync(CancellationToken cancellationToken = default);
}
