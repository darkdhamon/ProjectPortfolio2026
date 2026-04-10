namespace ProjectPortfolio2026.Server.Services.ServiceModels;

public sealed class ParsedWorkHistoryEntry
{
    public string? EmployerName { get; init; }

    public ParsedLocation? EmployerLocation { get; init; }

    public string? JobTitle { get; init; }

    public ParsedDateRange EmploymentDates { get; init; } = new();

    public string? SupervisorName { get; init; }

    public List<string> DescriptionLines { get; init; } = [];

    public string? DescriptionMarkdown { get; init; }

    public List<string> Skills { get; init; } = [];

    public List<string> Technologies { get; init; } = [];

    public List<string> Tags { get; init; } = [];

    public string? RawRoleText { get; init; }

    public Dictionary<string, string?> RawFields { get; init; } = new(StringComparer.Ordinal);
}
