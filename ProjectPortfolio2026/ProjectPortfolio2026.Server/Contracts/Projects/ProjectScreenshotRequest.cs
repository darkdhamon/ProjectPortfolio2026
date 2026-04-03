using System.ComponentModel.DataAnnotations;

namespace ProjectPortfolio2026.Server.Contracts.Projects;

public sealed class ProjectScreenshotRequest
{
    [Required]
    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Caption { get; set; }

    public int SortOrder { get; set; }
}
