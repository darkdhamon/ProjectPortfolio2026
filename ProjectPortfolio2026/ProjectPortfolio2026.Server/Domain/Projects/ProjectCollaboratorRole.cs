using System.ComponentModel.DataAnnotations;

namespace ProjectPortfolio2026.Server.Domain.Projects;

public sealed class ProjectCollaboratorRole
{
    public int Id { get; set; }

    public int ProjectCollaboratorId { get; set; }

    public ProjectCollaborator? ProjectCollaborator { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
