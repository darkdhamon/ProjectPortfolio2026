namespace ProjectPortfolio2026.ResumeParser.Models;

public sealed class ResumeSkillSection
{
    public string? Name { get; init; }

    public List<string> Items { get; init; } = [];
}
