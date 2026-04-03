namespace ProjectPortfolio2026.Server.Contracts.Projects;

public sealed class ProjectCollaboratorResponse
{
    public string Name { get; set; } = string.Empty;

    public string? GitHubProfileUrl { get; set; }

    public string? WebsiteUrl { get; set; }

    public string? PhotoUrl { get; set; }

    public List<string> Roles { get; set; } = [];
}
