namespace ProjectPortfolio2026.ResumeParser.Interfaces;

public interface IResumeSectionParser
{
    string SectionKey { get; }

    void Parse(IReadOnlyList<string>? lines, Implementations.ResumeDocumentBuilder builder);
}
