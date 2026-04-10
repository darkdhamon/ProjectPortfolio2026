using ProjectPortfolio2026.Server.Services.Interfaces;
using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Services.Implementations;

public sealed class DeferredResumeParserService : IResumeParserService
{
    public Task<ResumeParseResult> ParseAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("A source file name is required for resume parsing.", nameof(fileName));
        }

        throw new NotSupportedException(
            "No resume parser implementation is configured yet. Register a concrete IResumeParserService implementation before attempting resume import.");
    }
}
