using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Services.Interfaces;

public interface IResumeImportFileStore
{
    Task<StagedResumeFile> StageAsync(IFormFile file, CancellationToken cancellationToken = default);
}
