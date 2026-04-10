namespace ProjectPortfolio2026.Server.Services.ServiceModels;

public sealed class ParsedDateRange
{
    public string? StartDateText { get; init; }

    public string? EndDateText { get; init; }

    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public bool IsCurrentRole { get; init; }
}
