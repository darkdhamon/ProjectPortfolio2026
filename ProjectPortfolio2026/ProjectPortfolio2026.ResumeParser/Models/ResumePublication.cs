namespace ProjectPortfolio2026.ResumeParser.Models;

public sealed class ResumePublication
{
    public string? Title { get; init; }

    public string? Publisher { get; init; }

    public string? DateText { get; init; }

    public DateOnly? Date { get; init; }

    public string? Url { get; init; }

    public string? Description { get; init; }
}
