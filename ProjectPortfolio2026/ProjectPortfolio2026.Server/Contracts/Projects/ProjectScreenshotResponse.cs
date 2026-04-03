namespace ProjectPortfolio2026.Server.Contracts.Projects;

public sealed class ProjectScreenshotResponse
{
    public string ImageUrl { get; set; } = string.Empty;

    public string? Caption { get; set; }

    public int SortOrder { get; set; }
}
