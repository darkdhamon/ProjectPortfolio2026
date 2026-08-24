namespace ProjectPortfolio2026.ResumeParser.Interfaces;

public interface IResumeSectionClassifier
{
    Dictionary<string, List<string>> Classify(IReadOnlyList<string> lines);
}
