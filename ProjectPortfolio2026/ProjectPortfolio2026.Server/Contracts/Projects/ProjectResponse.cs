using ProjectPortfolio2026.Server.Contracts;

namespace ProjectPortfolio2026.Server.Contracts.Projects;

public sealed class ProjectResponse : ApiResponseDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? PrimaryImageUrl { get; set; }

    public string ShortDescription { get; set; } = string.Empty;

    public string LongDescriptionMarkdown { get; set; } = string.Empty;

    public string? GitHubUrl { get; set; }

    public string? DemoUrl { get; set; }

    public bool IsPublished { get; set; }

    public bool IsFeatured { get; set; }

    public List<ProjectScreenshotResponse> Screenshots { get; set; } = [];

    public List<string> DeveloperRoles { get; set; } = [];

    public List<string> Technologies { get; set; } = [];

    public List<string> Skills { get; set; } = [];

    public List<ProjectCollaboratorResponse> Collaborators { get; set; } = [];

    public List<ProjectMilestoneResponse> Milestones { get; set; } = [];
}
