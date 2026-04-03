namespace ProjectPortfolio2026.Server.Contracts.Projects;

public sealed class ProjectListItem
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? PrimaryImageUrl { get; set; }

    public string ShortDescription { get; set; } = string.Empty;

    public string? GitHubUrl { get; set; }

    public string? DemoUrl { get; set; }

    public bool IsFeatured { get; set; }

    public List<string> Skills { get; set; } = [];

    public List<string> Technologies { get; set; } = [];
}
