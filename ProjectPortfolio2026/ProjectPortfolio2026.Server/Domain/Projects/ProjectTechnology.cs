using System.ComponentModel.DataAnnotations;

namespace ProjectPortfolio2026.Server.Domain.Projects;

public sealed class ProjectTechnology
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public Project? Project { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
