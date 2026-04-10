namespace ProjectPortfolio2026.ResumeParser.Models;

public sealed class ResumeWorkExperienceEntry
{
    public string? EmployerName { get; init; }

    public ResumeLocation? EmployerLocation { get; init; }

    public string? JobTitle { get; init; }

    public ResumeDateRange EmploymentDates { get; init; } = new();

    public string? EmploymentType { get; init; }

    public string? SupervisorName { get; init; }

    public List<string> DescriptionLines { get; init; } = [];

    public string? DescriptionMarkdown { get; init; }

    public List<string> Skills { get; init; } = [];

    public List<string> Technologies { get; init; } = [];

    public List<string> Tags { get; init; } = [];

    public string? RawText { get; init; }

    public Dictionary<string, string?> Metadata { get; init; } = new(StringComparer.Ordinal);
}
