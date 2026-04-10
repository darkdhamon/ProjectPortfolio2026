namespace ProjectPortfolio2026.ResumeParser.Models;

public sealed class ResumeCertification
{
    public string? Name { get; init; }

    public string? Issuer { get; init; }

    public string? CredentialId { get; init; }

    public ResumeDateRange Dates { get; init; } = new();

    public string? Url { get; init; }

    public string? RawText { get; init; }
}
