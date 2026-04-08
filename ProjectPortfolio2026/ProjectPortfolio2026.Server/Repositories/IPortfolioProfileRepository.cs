using ProjectPortfolio2026.Server.Domain.Portfolio;

namespace ProjectPortfolio2026.Server.Repositories;

public interface IPortfolioProfileRepository
{
    Task<PortfolioProfile?> GetPublicAsync(CancellationToken cancellationToken = default);
}
