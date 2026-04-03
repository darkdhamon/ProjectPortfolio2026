namespace ProjectPortfolio2026.Server.Contracts.Projects;

public sealed class ProjectMilestoneResponse
{
    public string Title { get; set; } = string.Empty;

    public DateOnly? TargetDate { get; set; }

    public DateOnly? CompletedOn { get; set; }

    public string? Description { get; set; }
}
