namespace ProjectPortfolio2026.ResumeParser.Models;

public sealed class ResumeDateRange
{
    public string? StartDateText { get; init; }

    public string? EndDateText { get; init; }

    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public bool IsCurrent { get; init; }
}
