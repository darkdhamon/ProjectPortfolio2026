using ProjectPortfolio2026.Server.Domain.Portfolio;

namespace ProjectPortfolio2026.Server.Repositories;

public interface IResumeConfigurationRepository
{
    Task<ResumeConfiguration?> GetAsync(CancellationToken cancellationToken = default);

    Task<ResumeConfiguration> SaveAsync(ResumeConfiguration configuration, CancellationToken cancellationToken = default);
}
