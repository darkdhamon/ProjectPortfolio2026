namespace ProjectPortfolio2026.Server.Services.ServiceModels;

public sealed class ParsedPerson
{
    public string? FullName { get; init; }

    public string? FirstName { get; init; }

    public string? MiddleName { get; init; }

    public string? LastName { get; init; }

    public string? Headline { get; init; }

    public string? EmailAddress { get; init; }

    public List<string> PhoneNumbers { get; init; } = [];

    public ParsedLocation? Location { get; init; }

    public List<string> SocialProfiles { get; init; } = [];
}
