namespace ProjectPortfolio2026.Server.Services.ServiceModels;

public sealed class ResumeImportParseResult
{
    public ParsedPerson? Person { get; set; }

    public List<ParsedWorkHistoryEntry> WorkHistory { get; set; } = [];

    public List<string> GlobalSkills { get; set; } = [];

    public string? ProfessionalSummary { get; set; }

    public string? RawText { get; set; }

    public string? SourceFileName { get; set; }

    public string? ParserName { get; set; }
}
