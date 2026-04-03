namespace ProjectPortfolio2026.Server.Contracts.Projects;

public sealed class ProjectListPage
{
    public IReadOnlyList<ProjectListItem> Items { get; set; } = [];

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public bool HasMore { get; set; }

    public IReadOnlyList<string> AvailableSkills { get; set; } = [];
}
