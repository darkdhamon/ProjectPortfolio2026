using ProjectPortfolio2026.Server.Services.Interfaces;
using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Services.Implementations;

public sealed class DeferredResumeParserService : IResumeParserService
{
    public Task<ResumeImportParseResult> ParseAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResumeImportParseResult
        {
            SourceFileName = fileName,
            ParserName = nameof(DeferredResumeParserService)
        });
    }
}
