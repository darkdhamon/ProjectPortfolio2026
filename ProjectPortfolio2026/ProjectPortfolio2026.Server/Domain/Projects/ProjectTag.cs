using ProjectPortfolio2026.Server.Domain.Tags;

namespace ProjectPortfolio2026.Server.Domain.Projects;

public sealed class ProjectTag
{
    public int ProjectId { get; set; }

    public Project? Project { get; set; }

    public int TagId { get; set; }

    public Tag? Tag { get; set; }
}
