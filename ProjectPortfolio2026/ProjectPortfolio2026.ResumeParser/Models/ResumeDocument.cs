namespace ProjectPortfolio2026.ResumeParser.Models;

public sealed class ResumeDocument
{
    public ResumeHeader Header { get; init; } = new();

    public string? ProfessionalSummary { get; init; }

    public List<ResumeWorkExperienceEntry> WorkExperience { get; init; } = [];

    public List<ResumeEducationEntry> Education { get; init; } = [];

    public List<ResumeSkillSection> Skills { get; init; } = [];

    public List<ResumeCertification> Certifications { get; init; } = [];

    public List<ResumeProjectEntry> Projects { get; init; } = [];

    public List<ResumeLanguage> Languages { get; init; } = [];

    public List<ResumeAward> Awards { get; init; } = [];

    public List<ResumeVolunteerExperienceEntry> VolunteerExperience { get; init; } = [];

    public List<ResumePublication> Publications { get; init; } = [];

    public List<ResumeReference> References { get; init; } = [];

    public List<ResumeCustomSection> AdditionalSections { get; init; } = [];

    public string? RawText { get; init; }

    public string? SourceFileName { get; init; }

    public string? ParserName { get; init; }

    public Dictionary<string, string?> Metadata { get; init; } = new(StringComparer.Ordinal);
}
