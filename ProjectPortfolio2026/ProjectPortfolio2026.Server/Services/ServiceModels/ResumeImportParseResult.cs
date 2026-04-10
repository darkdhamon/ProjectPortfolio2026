namespace ProjectPortfolio2026.Server.Services.ServiceModels;

public sealed class ResumeImportParseResult
{
    public ParsedPerson? Person { get; init; }

    public List<ParsedWorkHistoryEntry> WorkHistory { get; init; } = [];

    public List<string> GlobalSkills { get; init; } = [];

    public string? ProfessionalSummary { get; init; }

    public string? RawText { get; init; }

    public string? SourceFileName { get; init; }

    public string? ParserName { get; init; }
}
