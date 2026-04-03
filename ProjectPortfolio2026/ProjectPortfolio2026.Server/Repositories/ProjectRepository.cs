using Microsoft.EntityFrameworkCore;
using ProjectPortfolio2026.Server.Data;
using ProjectPortfolio2026.Server.Contracts.Projects;
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

    public async Task<ProjectListPage> ListAsync(
        string? search,
        IReadOnlyCollection<string> skillFilters,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
        var normalizedSearch = search?.Trim();
        var normalizedSkillFilters = skillFilters
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Select(skill => skill.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var query = CreateProjectQuery()
            .Where(project => project.IsPublished);

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(project =>
                EF.Functions.Like(project.Title, $"%{normalizedSearch}%") ||
                EF.Functions.Like(project.ShortDescription, $"%{normalizedSearch}%") ||
                EF.Functions.Like(project.LongDescriptionMarkdown, $"%{normalizedSearch}%") ||
                project.Technologies.Any(technology => EF.Functions.Like(technology.Name, $"%{normalizedSearch}%")) ||
                project.Skills.Any(skill => EF.Functions.Like(skill.Name, $"%{normalizedSearch}%")));
        }

        if (normalizedSkillFilters.Count > 0)
        {
            foreach (var filter in normalizedSkillFilters)
            {
                var skillFilter = filter;
                query = query.Where(project =>
                    project.Skills.Any(skill => skill.Name.ToUpper() == skillFilter));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(project => project.EndDate.HasValue ? 1 : 0)
            .ThenByDescending(project => project.EndDate ?? project.StartDate)
            .ThenByDescending(project => project.StartDate)
            .ThenBy(project => project.Title)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(project => new ProjectListItem
            {
                Id = project.Id,
                Title = project.Title,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                PrimaryImageUrl = project.PrimaryImageUrl,
                ShortDescription = project.ShortDescription,
                GitHubUrl = project.GitHubUrl,
                DemoUrl = project.DemoUrl,
                IsFeatured = project.IsFeatured,
                Skills = project.Skills
                    .Select(skill => skill.Name)
                    .OrderBy(skill => skill)
                    .ToList(),
                Technologies = project.Technologies
                    .Select(technology => technology.Name)
                    .OrderBy(technology => technology)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var availableSkills = await CreateProjectQuery()
            .Where(project => project.IsPublished)
            .SelectMany(project => project.Skills.Select(skill => skill.Name))
            .Distinct()
            .OrderBy(skill => skill)
            .ToListAsync(cancellationToken);

        return new ProjectListPage
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = totalCount,
            HasMore = (normalizedPage * normalizedPageSize) < totalCount,
            AvailableSkills = availableSkills
        };
    }

    public async Task<IReadOnlyList<ProjectListItem>> ListFeaturedAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 5);
        var publishedProjects = await CreateProjectQuery()
            .Where(project => project.IsPublished)
            .OrderByDescending(project => project.StartDate)
            .ThenBy(project => project.Title)
            .Select(project => new ProjectListItem
            {
                Id = project.Id,
                Title = project.Title,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                PrimaryImageUrl = project.PrimaryImageUrl,
                ShortDescription = project.ShortDescription,
                GitHubUrl = project.GitHubUrl,
                DemoUrl = project.DemoUrl,
                IsFeatured = project.IsFeatured,
                Skills = project.Skills
                    .Select(skill => skill.Name)
                    .OrderBy(skill => skill)
                    .ToList(),
                Technologies = project.Technologies
                    .Select(technology => technology.Name)
                    .OrderBy(technology => technology)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var featuredProjects = publishedProjects
            .Where(project => project.IsFeatured)
            .ToList();
        var selectedProjects = featuredProjects.Count > normalizedLimit
            ? Shuffle(featuredProjects).Take(normalizedLimit).ToList()
            : featuredProjects.Take(normalizedLimit).ToList();

        if (selectedProjects.Count < normalizedLimit)
        {
            var selectedIds = selectedProjects
                .Select(project => project.Id)
                .ToHashSet();
            var fallbackProjects = publishedProjects
                .Where(project => !selectedIds.Contains(project.Id))
                .Take(normalizedLimit - selectedProjects.Count);

            selectedProjects.AddRange(fallbackProjects);
        }

        return selectedProjects;
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

    private static IEnumerable<TItem> Shuffle<TItem>(IReadOnlyList<TItem> items)
    {
        var shuffledItems = items.ToList();

        for (var index = shuffledItems.Count - 1; index > 0; index -= 1)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (shuffledItems[index], shuffledItems[swapIndex]) = (shuffledItems[swapIndex], shuffledItems[index]);
        }

        return shuffledItems;
    }
}
