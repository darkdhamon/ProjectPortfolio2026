using System.ComponentModel.DataAnnotations;

namespace ProjectPortfolio2026.Server.Contracts.Projects;

public sealed class ProjectRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [MaxLength(500)]
    public string? PrimaryImageUrl { get; set; }

    [Required]
    [MaxLength(350)]
    public string ShortDescription { get; set; } = string.Empty;

    [Required]
    public string LongDescriptionMarkdown { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? GitHubUrl { get; set; }

    [MaxLength(500)]
    public string? DemoUrl { get; set; }

    public bool IsPublished { get; set; }

    public bool IsFeatured { get; set; }

    public List<ProjectScreenshotRequest> Screenshots { get; set; } = [];

    public List<string> DeveloperRoles { get; set; } = [];

    public List<string> Technologies { get; set; } = [];

    public List<string> Skills { get; set; } = [];

    public List<ProjectCollaboratorRequest> Collaborators { get; set; } = [];

    public List<ProjectMilestoneRequest> Milestones { get; set; } = [];
}
