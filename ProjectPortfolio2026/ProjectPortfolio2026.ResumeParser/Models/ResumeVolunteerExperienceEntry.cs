namespace ProjectPortfolio2026.ResumeParser.Models;

public sealed class ResumeVolunteerExperienceEntry
{
    public string? OrganizationName { get; init; }

    public string? RoleTitle { get; init; }

    public ResumeDateRange Dates { get; init; } = new();

    public List<string> DescriptionLines { get; init; } = [];

    public string? RawText { get; init; }
}
