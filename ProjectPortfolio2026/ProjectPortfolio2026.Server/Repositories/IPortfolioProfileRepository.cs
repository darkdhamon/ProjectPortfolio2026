using ProjectPortfolio2026.Server.Domain.Portfolio;

namespace ProjectPortfolio2026.Server.Repositories;

public interface IPortfolioProfileRepository
{
    Task<PortfolioProfile?> GetPublicAsync(CancellationToken cancellationToken = default);

    Task<List<PortfolioSocialLink>> GetSocialLinksAsync(CancellationToken cancellationToken = default);

    Task<List<PortfolioSocialLink>> SaveSocialLinksAsync(IEnumerable<PortfolioSocialLink> socialLinks, CancellationToken cancellationToken = default);
}
