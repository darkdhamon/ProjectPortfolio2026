using ProjectPortfolio2026.Server.Domain.Projects;

namespace ProjectPortfolio2026.Server.Repositories;

public interface IProjectRepository
{
    Task<Project> AddAsync(Project project, CancellationToken cancellationToken = default);

    Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken = default);

    Task<Project?> UpdateAsync(Project project, CancellationToken cancellationToken = default);
}
