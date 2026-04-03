using System.ComponentModel.DataAnnotations;

namespace ProjectPortfolio2026.Server.Contracts.Projects;

public sealed class ProjectMilestoneRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public DateOnly? TargetDate { get; set; }

    public DateOnly? CompletedOn { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
}
