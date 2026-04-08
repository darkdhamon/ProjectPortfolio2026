using Microsoft.EntityFrameworkCore;
using ProjectPortfolio2026.Server.Data;
using ProjectPortfolio2026.Server.Domain.Projects;
using ProjectPortfolio2026.Server.Domain.Tags;
using ProjectPortfolio2026.Server.Services.Interfaces;

namespace ProjectPortfolio2026.Server.Services.Implementations;

public sealed class ProjectTagNormalizer(PortfolioDbContext dbContext) : IProjectTagNormalizer
{
    public async Task NormalizeAsync(Project project, CancellationToken cancellationToken = default)
    {
        if (project.ProjectTags.Count == 0)
        {
            return;
        }

        var requestedTags = project.ProjectTags
            .Select(projectTag => projectTag.Tag)
            .Where(tag => tag is not null)
            .Select(tag => new TagKey(
                tag!.Category,
                tag.DisplayName.Trim(),
                string.IsNullOrWhiteSpace(tag.NormalizedName) ? NormalizeTagName(tag.DisplayName) : NormalizeTagName(tag.NormalizedName)))
            .Where(tag => tag.DisplayName.Length > 0)
            .GroupBy(tag => new { tag.Category, tag.NormalizedName })
            .Select(group => group.First())
            .ToList();

        var requestedCategories = requestedTags
            .Select(tag => tag.Category)
            .Distinct()
            .ToList();

        var existingTags = await dbContext.Tags
            .Where(tag => requestedCategories.Contains(tag.Category))
            .ToListAsync(cancellationToken);

        project.ProjectTags = requestedTags
            .Select(requestedTag => new ProjectTag
            {
                ProjectId = project.Id,
                Tag = ResolveTag(existingTags, requestedTag)
            })
            .ToList();
    }

    private static Tag ResolveTag(ICollection<Tag> existingTags, TagKey requestedTag)
    {
        var resolvedTag = existingTags.SingleOrDefault(tag =>
            tag.Category == requestedTag.Category &&
            tag.NormalizedName == requestedTag.NormalizedName);

        if (resolvedTag is not null)
        {
            return resolvedTag;
        }

        resolvedTag = new Tag
        {
            Category = requestedTag.Category,
            DisplayName = requestedTag.DisplayName,
            NormalizedName = requestedTag.NormalizedName
        };

        existingTags.Add(resolvedTag);
        return resolvedTag;
    }

    private static string NormalizeTagName(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private sealed record TagKey(TagCategory Category, string DisplayName, string NormalizedName);
}
