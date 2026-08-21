namespace ProjectPortfolio2026.ResumeParser.Models;

public sealed class ResumeAward
{
    public string? Title { get; init; }

    public string? Issuer { get; init; }

    public string? DateText { get; init; }

    public DateOnly? Date { get; init; }

    public string? Description { get; init; }
}
