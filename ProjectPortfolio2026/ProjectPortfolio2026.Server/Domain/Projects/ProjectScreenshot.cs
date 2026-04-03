using System.ComponentModel.DataAnnotations;

namespace ProjectPortfolio2026.Server.Domain.Projects;

public sealed class ProjectScreenshot
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public Project? Project { get; set; }

    [Required]
    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Caption { get; set; }

    public int SortOrder { get; set; }
}
