namespace ProjectPortfolio2026.ResumeParser.Models;

public sealed class ResumeHeader
{
    public string? FullName { get; init; }

    public string? FirstName { get; init; }

    public string? MiddleName { get; init; }

    public string? LastName { get; init; }

    public string? Headline { get; init; }

    public string? EmailAddress { get; init; }

    public List<string> PhoneNumbers { get; init; } = [];

    public ResumeLocation? Location { get; init; }

    public List<ResumeProfileLink> Profiles { get; init; } = [];
}
