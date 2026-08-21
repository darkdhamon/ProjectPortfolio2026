using ProjectPortfolio2026.ResumeParser.Models;

namespace ProjectPortfolio2026.ResumeParser.Interfaces;

public interface IResumeDocumentParser
{
    Task<ResumeDocument> ParseAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default);
}
