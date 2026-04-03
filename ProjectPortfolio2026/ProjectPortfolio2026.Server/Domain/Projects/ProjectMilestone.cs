using System.ComponentModel.DataAnnotations;

namespace ProjectPortfolio2026.Server.Domain.Projects;

public sealed class ProjectMilestone
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public Project? Project { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public DateOnly? TargetDate { get; set; }

    public DateOnly? CompletedOn { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
}
