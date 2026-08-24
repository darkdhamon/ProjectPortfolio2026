namespace ProjectPortfolio2026.Server.Services.ServiceModels;

public sealed class ParsedLocation
{
    public string? StreetAddress1 { get; init; }

    public string? StreetAddress2 { get; init; }

    public string? City { get; init; }

    public string? Region { get; init; }

    public string? PostalCode { get; init; }

    public string? Country { get; init; }

    public string? DisplayText { get; init; }
}
