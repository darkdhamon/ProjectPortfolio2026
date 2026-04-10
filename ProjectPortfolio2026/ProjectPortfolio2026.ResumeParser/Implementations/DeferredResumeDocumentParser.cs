using ProjectPortfolio2026.ResumeParser.Interfaces;
using ProjectPortfolio2026.ResumeParser.Models;

namespace ProjectPortfolio2026.ResumeParser.Implementations;

public sealed class DeferredResumeDocumentParser : IResumeDocumentParser
{
    public Task<ResumeDocument> ParseAsync(
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
            "No resume document parser implementation is configured yet. Register a concrete IResumeDocumentParser implementation before attempting resume import.");
    }
}
