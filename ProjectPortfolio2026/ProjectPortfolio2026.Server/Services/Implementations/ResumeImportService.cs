using ProjectPortfolio2026.Server.Services.Interfaces;
using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Services.Implementations;

public sealed class ResumeImportService(
    IResumeImportFileStore resumeImportFileStore,
    IResumeParserService resumeParserService) : IResumeImportService
{
    public async Task<ResumeImportCandidateResult> ParseAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        await using var stagedFile = await resumeImportFileStore.StageAsync(file, cancellationToken);
        await using var content = File.OpenRead(stagedFile.StoredFilePath);

        var result = await resumeParserService.ParseAsync(content, stagedFile.OriginalFileName, cancellationToken);
        result.SourceFileName ??= stagedFile.OriginalFileName;

        return ResumeImportCandidateNormalizer.Normalize(result);
    }
}
