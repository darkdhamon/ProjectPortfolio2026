using ProjectPortfolio2026.Server.Contracts.Projects;

namespace ProjectPortfolio2026.Server.Services.Interfaces;

public interface IFeaturedProjectSelector
{
    IReadOnlyList<ProjectListItem> Select(IReadOnlyList<ProjectListItem> publishedProjects, int limit);
}
