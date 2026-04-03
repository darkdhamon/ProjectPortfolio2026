using System.ComponentModel.DataAnnotations;

namespace ProjectPortfolio2026.Server.Domain.Projects;

public sealed class ProjectCollaborator
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public Project? Project { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? GitHubProfileUrl { get; set; }

    [MaxLength(500)]
    public string? WebsiteUrl { get; set; }

    [MaxLength(500)]
    public string? PhotoUrl { get; set; }

    public List<ProjectCollaboratorRole> Roles { get; set; } = [];
}
