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
}
