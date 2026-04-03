using ProjectPortfolio2026.Server.Domain.Projects;
using ProjectPortfolio2026.Server.Contracts.Projects;

namespace ProjectPortfolio2026.Server.Repositories;

public interface IProjectRepository
{
    Task<Project> AddAsync(Project project, CancellationToken cancellationToken = default);

    Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ProjectListPage> ListAsync(
        string? search,
        IReadOnlyCollection<string> skillFilters,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Project?> UpdateAsync(Project project, CancellationToken cancellationToken = default);
}
