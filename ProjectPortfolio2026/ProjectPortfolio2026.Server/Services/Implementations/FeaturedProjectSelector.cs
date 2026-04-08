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
            .ToList();
        var selectedProjects = featuredProjects.Count > normalizedLimit
            ? Shuffle(featuredProjects).Take(normalizedLimit).ToList()
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

    private static IEnumerable<TItem> Shuffle<TItem>(IReadOnlyList<TItem> items)
    {
        var shuffledItems = items.ToList();

        for (var index = shuffledItems.Count - 1; index > 0; index -= 1)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (shuffledItems[index], shuffledItems[swapIndex]) = (shuffledItems[swapIndex], shuffledItems[index]);
        }

        return shuffledItems;
    }
}
