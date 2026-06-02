namespace ProjectPortfolio2026.ResumeParser.Models;

public sealed class ResumeEducationEntry
{
    public string? InstitutionName { get; init; }

    public ResumeLocation? InstitutionLocation { get; init; }

    public string? Degree { get; init; }

    public string? FieldOfStudy { get; init; }

    public ResumeDateRange AttendanceDates { get; init; } = new();

    public List<string> DescriptionLines { get; init; } = [];

    public string? RawText { get; init; }
}
