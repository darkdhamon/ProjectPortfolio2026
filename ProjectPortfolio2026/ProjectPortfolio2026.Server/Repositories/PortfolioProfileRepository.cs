using Microsoft.EntityFrameworkCore;
using ProjectPortfolio2026.Server.Data;
using ProjectPortfolio2026.Server.Domain.Portfolio;

namespace ProjectPortfolio2026.Server.Repositories;

public sealed class PortfolioProfileRepository(PortfolioDbContext dbContext) : IPortfolioProfileRepository
{
    public async Task<PortfolioProfile?> GetPublicAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.PortfolioProfiles
            .AsNoTracking()
            .Include(profile => profile.ContactMethods)
            .Include(profile => profile.SocialLinks)
            .Where(profile => profile.IsPublic)
            .OrderByDescending(profile => profile.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<PortfolioSocialLink>> GetSocialLinksAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.PortfolioProfiles
            .AsNoTracking()
            .Include(profile => profile.SocialLinks)
            .Where(profile => profile.IsPublic)
            .OrderByDescending(profile => profile.Id)
            .Select(profile => profile.SocialLinks)
            .FirstOrDefaultAsync(cancellationToken) ?? [];
    }

    public async Task<List<PortfolioSocialLink>> SaveSocialLinksAsync(
        IEnumerable<PortfolioSocialLink> socialLinks,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.PortfolioProfiles
            .Include(profile => profile.SocialLinks)
            .Where(profile => profile.IsPublic)
            .OrderByDescending(profile => profile.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return [];
        }

        var sortedLinks = socialLinks
            .Select((link, index) =>
            {
                link.SortOrder = link.SortOrder == 0 ? index + 1 : link.SortOrder;
                return link;
            })
            .ToList();

        var requestedIds = sortedLinks
            .Select(link => link.Id)
            .Where(linkId => linkId > 0)
            .ToHashSet();

        foreach (var existingLink in profile.SocialLinks
            .Where(existingLink => existingLink.Id > 0 && !requestedIds.Contains(existingLink.Id))
            .ToList())
        {
            dbContext.Entry(existingLink).State = EntityState.Deleted;
        }

        foreach (var sortedLink in sortedLinks)
        {
            if (sortedLink.Id > 0)
            {
                var existing = profile.SocialLinks.FirstOrDefault(link => link.Id == sortedLink.Id);
                if (existing is not null)
                {
                    existing.Platform = sortedLink.Platform;
                    existing.Label = sortedLink.Label;
                    existing.Url = sortedLink.Url;
                    existing.Handle = sortedLink.Handle;
                    existing.Summary = sortedLink.Summary;
                    existing.SortOrder = sortedLink.SortOrder;
                    existing.IsVisible = sortedLink.IsVisible;
                    continue;
                }
            }

            sortedLink.Id = 0;
            profile.SocialLinks.Add(sortedLink);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return profile.SocialLinks
            .OrderBy(link => link.SortOrder)
            .ThenBy(link => link.Label)
            .ToList();
    }
}
