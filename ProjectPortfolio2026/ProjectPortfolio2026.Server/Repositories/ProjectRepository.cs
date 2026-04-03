using Microsoft.EntityFrameworkCore;
using ProjectPortfolio2026.Server.Data;
using ProjectPortfolio2026.Server.Domain.Projects;

namespace ProjectPortfolio2026.Server.Repositories;

public sealed class ProjectRepository(PortfolioDbContext dbContext) : IProjectRepository
{
    public async Task<Project> AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRequiredProjectAsync(project.Id, cancellationToken);
    }

    public async Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await CreateProjectQuery()
            .SingleOrDefaultAsync(project => project.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await CreateProjectQuery()
            .OrderByDescending(project => project.StartDate)
            .ThenBy(project => project.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<Project?> UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        var existingProject = await dbContext.Projects
            .Include(existing => existing.Screenshots)
            .Include(existing => existing.DeveloperRoles)
            .Include(existing => existing.Technologies)
            .Include(existing => existing.Skills)
            .Include(existing => existing.Collaborators)
                .ThenInclude(collaborator => collaborator.Roles)
            .Include(existing => existing.Milestones)
            .SingleOrDefaultAsync(existing => existing.Id == project.Id, cancellationToken);

        if (existingProject is null)
        {
            return null;
        }

        dbContext.Entry(existingProject).CurrentValues.SetValues(project);
        ReplaceCollection(existingProject.Screenshots, project.Screenshots);
        ReplaceCollection(existingProject.DeveloperRoles, project.DeveloperRoles);
        ReplaceCollection(existingProject.Technologies, project.Technologies);
        ReplaceCollection(existingProject.Skills, project.Skills);
        ReplaceCollection(existingProject.Milestones, project.Milestones);
        ReplaceCollaborators(existingProject.Collaborators, project.Collaborators);

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRequiredProjectAsync(project.Id, cancellationToken);
    }

    private IQueryable<Project> CreateProjectQuery()
    {
        return dbContext.Projects
            .AsNoTracking()
            .Include(project => project.Screenshots)
            .Include(project => project.DeveloperRoles)
            .Include(project => project.Technologies)
            .Include(project => project.Skills)
            .Include(project => project.Collaborators)
                .ThenInclude(collaborator => collaborator.Roles)
            .Include(project => project.Milestones);
    }

    private async Task<Project> GetRequiredProjectAsync(int id, CancellationToken cancellationToken)
    {
        return await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Project {id} was expected to exist after persistence.");
    }

    private static void ReplaceCollection<TItem>(ICollection<TItem> target, IEnumerable<TItem> source)
    {
        target.Clear();

        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private static void ReplaceCollaborators(
        ICollection<ProjectCollaborator> target,
        IEnumerable<ProjectCollaborator> source)
    {
        target.Clear();

        foreach (var collaborator in source)
        {
            collaborator.Roles ??= [];
            target.Add(collaborator);
        }
    }
}
