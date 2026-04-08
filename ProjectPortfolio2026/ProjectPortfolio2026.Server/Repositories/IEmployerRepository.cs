using ProjectPortfolio2026.Server.Domain.WorkHistory;

namespace ProjectPortfolio2026.Server.Repositories;

public interface IEmployerRepository
{
    Task<IReadOnlyList<Employer>> ListPublishedAsync(CancellationToken cancellationToken = default);
}
