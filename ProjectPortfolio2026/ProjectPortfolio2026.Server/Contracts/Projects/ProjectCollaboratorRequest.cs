using System.ComponentModel.DataAnnotations;

namespace ProjectPortfolio2026.Server.Contracts.Projects;

public sealed class ProjectCollaboratorRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? GitHubProfileUrl { get; set; }

    [MaxLength(500)]
    public string? WebsiteUrl { get; set; }

    [MaxLength(500)]
    public string? PhotoUrl { get; set; }

    public List<string> Roles { get; set; } = [];
}
