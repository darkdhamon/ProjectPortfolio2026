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

public sealed class ParsedPerson
{
    public string? FullName { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? LastName { get; set; }

    public string? Headline { get; set; }

    public string? EmailAddress { get; set; }

    public List<string> PhoneNumbers { get; set; } = [];

    public ParsedLocation? Location { get; set; }

    public List<string> SocialProfiles { get; set; } = [];
}

public sealed class ParsedLocation
{
    public string? StreetAddress1 { get; set; }

    public string? StreetAddress2 { get; set; }

    public string? City { get; set; }

    public string? Region { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public string? DisplayText { get; set; }
}

public sealed class ParsedWorkHistoryEntry
{
    public string? EmployerName { get; set; }

    public ParsedLocation? EmployerLocation { get; set; }

    public string? JobTitle { get; set; }

    public string? EmploymentType { get; set; }

    public string? SupervisorName { get; set; }

    public ParsedDateRange EmploymentDates { get; set; } = new();

    public List<string> DescriptionLines { get; set; } = [];

    public string? DescriptionMarkdown { get; set; }

    public List<string> Skills { get; set; } = [];

    public List<string> Technologies { get; set; } = [];

    public List<string> Tags { get; set; } = [];

    public string? RawRoleText { get; set; }

    public Dictionary<string, string?> RawFields { get; set; } = new(StringComparer.Ordinal);
}

public sealed class ParsedDateRange
{
    public string? StartDateText { get; set; }

    public string? EndDateText { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsCurrentRole { get; set; }
}
