using System.ComponentModel.DataAnnotations;

namespace ProjectPortfolio2026.Server.Domain.Projects;

public sealed class Project
{
    public int Id { get; set; }

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

    public List<ProjectScreenshot> Screenshots { get; set; } = [];

    public List<ProjectDeveloperRole> DeveloperRoles { get; set; } = [];

    public List<ProjectTechnology> Technologies { get; set; } = [];

    public List<ProjectSkill> Skills { get; set; } = [];

    public List<ProjectCollaborator> Collaborators { get; set; } = [];

    public List<ProjectMilestone> Milestones { get; set; } = [];
}
