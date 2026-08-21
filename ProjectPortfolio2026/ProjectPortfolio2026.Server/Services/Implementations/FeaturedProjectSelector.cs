using ProjectPortfolio2026.Server.Contracts.Projects;
using ProjectPortfolio2026.Server.Services.Interfaces;

namespace ProjectPortfolio2026.Server.Services.Implementations;

public sealed class FeaturedProjectSelector : IFeaturedProjectSelector
{
    public IReadOnlyList<ProjectListItem> Select(IReadOnlyList<ProjectListItem> publishedProjects, int limit)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 5);
        var featuredProjects = publishedProjects
            .Where(project => project.IsFeatured)
            .OrderBy(project => project.FeaturedOrder.HasValue ? 0 : 1)
            .ThenBy(project => project.FeaturedOrder ?? int.MaxValue)
            .ThenByDescending(project => project.StartDate)
            .ThenBy(project => project.Title)
            .ToList();
        var selectedProjects = featuredProjects.Count > normalizedLimit
            ? featuredProjects.Take(normalizedLimit).ToList()
            : featuredProjects.Take(normalizedLimit).ToList();

        if (selectedProjects.Count < normalizedLimit)
        {
            var selectedIds = selectedProjects
                .Select(project => project.Id)
                .ToHashSet();
            var fallbackProjects = publishedProjects
                .Where(project => !selectedIds.Contains(project.Id))
                .Take(normalizedLimit - selectedProjects.Count);

            selectedProjects.AddRange(fallbackProjects);
        }

        return selectedProjects;
    }
}
