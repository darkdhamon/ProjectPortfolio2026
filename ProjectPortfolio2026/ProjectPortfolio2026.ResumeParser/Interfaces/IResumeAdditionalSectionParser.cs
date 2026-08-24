namespace ProjectPortfolio2026.ResumeParser.Interfaces;

public interface IResumeAdditionalSectionParser
{
    List<Models.ResumeCustomSection> Parse(IReadOnlyDictionary<string, List<string>> sections);
}
