using Microsoft.EntityFrameworkCore;

using ProjectPortfolio2026.Server.Data;
using ProjectPortfolio2026.Server.Domain.WorkHistory;

namespace ProjectPortfolio2026.Server.Repositories;

public sealed class EmployerRepository(PortfolioDbContext dbContext) : IEmployerRepository
{
    public async Task<IReadOnlyList<Employer>> ListPublishedAsync(CancellationToken cancellationToken = default)
    {
        var employers = await dbContext.Employers
            .AsNoTracking()
            .Where(employer => employer.IsPublished)
            .Include(employer => employer.JobRoles)
                .ThenInclude(jobRole => jobRole.JobRoleTags)
                    .ThenInclude(jobRoleTag => jobRoleTag.Tag)
            .ToListAsync(cancellationToken);

        return employers
            .OrderByDescending(employer => employer.JobRoles.Any(jobRole => !jobRole.EndDate.HasValue))
            .ThenByDescending(employer => employer.JobRoles
                .Select(jobRole => jobRole.EndDate ?? jobRole.StartDate)
                .DefaultIfEmpty(DateOnly.MinValue)
                .Max())
            .ThenBy(employer => employer.Name)
            .ToList();
    }
}
