namespace ProjectPortfolio2026.ResumeParser.Interfaces;

public interface IResumeTextExtractor
{
    bool CanExtract(string fileName);

    Task<string> ExtractAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
}
