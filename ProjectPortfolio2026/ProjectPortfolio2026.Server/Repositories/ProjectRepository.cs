using Microsoft.EntityFrameworkCore;
using System.Data;
using ProjectPortfolio2026.Server.Data;
using ProjectPortfolio2026.Server.Contracts.Projects;
using ProjectPortfolio2026.Server.Domain.Projects;
using ProjectPortfolio2026.Server.Domain.Tags;
using ProjectPortfolio2026.Server.Services.Interfaces;

namespace ProjectPortfolio2026.Server.Repositories;

public sealed class ProjectRepository(
    PortfolioDbContext dbContext,
    IProjectTagNormalizer projectTagNormalizer,
    IFeaturedProjectSelector featuredProjectSelector) : IProjectRepository
{
    private static readonly SemaphoreSlim FeaturedOrderLock = new(1, 1);

    public async Task<Project> AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await projectTagNormalizer.NormalizeAsync(project, cancellationToken);
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRequiredProjectAsync(project.Id, cancellationToken);
    }

    public async Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await CreateProjectQuery()
            .SingleOrDefaultAsync(project => project.Id == id && !project.IsArchived, cancellationToken);
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
            .Where(project => project.IsPublished && !project.IsArchived);

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(project =>
                EF.Functions.Like(project.Title, $"%{normalizedSearch}%") ||
                EF.Functions.Like(project.ShortDescription, $"%{normalizedSearch}%") ||
                EF.Functions.Like(project.LongDescriptionMarkdown, $"%{normalizedSearch}%") ||
                project.ProjectTags.Any(projectTag => EF.Functions.Like(projectTag.Tag!.DisplayName, $"%{normalizedSearch}%")));
        }

        if (normalizedSkillFilters.Count > 0)
        {
            foreach (var filter in normalizedSkillFilters)
            {
                var skillFilter = filter;
                query = query.Where(project =>
                    project.ProjectTags.Any(projectTag =>
                        projectTag.Tag!.Category == TagCategory.Skill &&
                        projectTag.Tag.NormalizedName == skillFilter));
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
                IsPublished = project.IsPublished,
                IsFeatured = project.IsFeatured,
                FeaturedOrder = project.FeaturedOrder,
                IsArchived = project.IsArchived,
                ArchivedAt = project.ArchivedAt,
                Skills = project.ProjectTags
                    .Where(projectTag => projectTag.Tag!.Category == TagCategory.Skill)
                    .Select(projectTag => projectTag.Tag!.DisplayName)
                    .OrderBy(skill => skill)
                    .ToList(),
                Technologies = project.ProjectTags
                    .Where(projectTag => projectTag.Tag!.Category == TagCategory.Technology)
                    .Select(projectTag => projectTag.Tag!.DisplayName)
                    .OrderBy(technology => technology)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var availableSkills = await CreateProjectQuery()
            .Where(project => project.IsPublished)
            .Where(project => !project.IsArchived)
            .SelectMany(project => project.ProjectTags
                .Where(projectTag => projectTag.Tag!.Category == TagCategory.Skill)
                .Select(projectTag => projectTag.Tag!.DisplayName))
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

    public async Task<ProjectListPage> ListAdminAsync(
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

        var query = CreateProjectQuery();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(project =>
                EF.Functions.Like(project.Title, $"%{normalizedSearch}%") ||
                EF.Functions.Like(project.ShortDescription, $"%{normalizedSearch}%") ||
                EF.Functions.Like(project.LongDescriptionMarkdown, $"%{normalizedSearch}%") ||
                project.ProjectTags.Any(projectTag => EF.Functions.Like(projectTag.Tag!.DisplayName, $"%{normalizedSearch}%")));
        }

        if (normalizedSkillFilters.Count > 0)
        {
            foreach (var filter in normalizedSkillFilters)
            {
                var skillFilter = filter;
                query = query.Where(project =>
                    project.ProjectTags.Any(projectTag =>
                        projectTag.Tag!.Category == TagCategory.Skill &&
                        projectTag.Tag.NormalizedName == skillFilter));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(project => project.EndDate.HasValue ? 1 : 0)
            .ThenByDescending(project => project.EndDate ?? project.StartDate)
            .ThenByDescending(project => project.StartDate)
            .ThenBy(project => project.Title)
            .ThenBy(project => project.Id)
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
                IsPublished = project.IsPublished,
                IsFeatured = project.IsFeatured,
                FeaturedOrder = project.FeaturedOrder,
                IsArchived = project.IsArchived,
                ArchivedAt = project.ArchivedAt,
                Skills = project.ProjectTags
                    .Where(projectTag => projectTag.Tag!.Category == TagCategory.Skill)
                    .Select(projectTag => projectTag.Tag!.DisplayName)
                    .OrderBy(skill => skill)
                    .ToList(),
                Technologies = project.ProjectTags
                    .Where(projectTag => projectTag.Tag!.Category == TagCategory.Technology)
                    .Select(projectTag => projectTag.Tag!.DisplayName)
                    .OrderBy(technology => technology)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var availableSkills = await CreateProjectQuery()
            .SelectMany(project => project.ProjectTags
                .Where(projectTag => projectTag.Tag!.Category == TagCategory.Skill)
                .Select(projectTag => projectTag.Tag!.DisplayName))
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
        var publishedProjects = await CreateProjectQuery()
            .Where(project => project.IsPublished && !project.IsArchived)
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
                IsPublished = project.IsPublished,
                IsFeatured = project.IsFeatured,
                FeaturedOrder = project.FeaturedOrder,
                IsArchived = project.IsArchived,
                ArchivedAt = project.ArchivedAt,
                Skills = project.ProjectTags
                    .Where(projectTag => projectTag.Tag!.Category == TagCategory.Skill)
                    .Select(projectTag => projectTag.Tag!.DisplayName)
                    .OrderBy(skill => skill)
                    .ToList(),
                Technologies = project.ProjectTags
                    .Where(projectTag => projectTag.Tag!.Category == TagCategory.Technology)
                    .Select(projectTag => projectTag.Tag!.DisplayName)
                    .OrderBy(technology => technology)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return featuredProjectSelector.Select(publishedProjects, limit);
    }

    public async Task<IReadOnlyList<ProjectListItem>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await CreateProjectQuery()
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
                IsPublished = project.IsPublished,
                IsFeatured = project.IsFeatured,
                FeaturedOrder = project.FeaturedOrder,
                Skills = project.ProjectTags
                    .Where(projectTag => projectTag.Tag!.Category == TagCategory.Skill)
                    .Select(projectTag => projectTag.Tag!.DisplayName)
                    .OrderBy(skill => skill)
                    .ToList(),
                Technologies = project.ProjectTags
                    .Where(projectTag => projectTag.Tag!.Category == TagCategory.Technology)
                    .Select(projectTag => projectTag.Tag!.DisplayName)
                    .OrderBy(technology => technology)
                    .ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<Project?> UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        await projectTagNormalizer.NormalizeAsync(project, cancellationToken);

        var existingProject = await dbContext.Projects
            .Include(existing => existing.Screenshots)
            .Include(existing => existing.DeveloperRoles)
            .Include(existing => existing.ProjectTags)
                .ThenInclude(projectTag => projectTag.Tag)
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
        ReplaceCollection(existingProject.ProjectTags, project.ProjectTags);
        ReplaceCollection(existingProject.Milestones, project.Milestones);
        ReplaceCollaborators(existingProject.Collaborators, project.Collaborators);

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRequiredProjectAsync(project.Id, cancellationToken);
    }

    public async Task<Project?> UpdateFeaturedStateAsync(
        int projectId,
        bool isFeatured,
        CancellationToken cancellationToken = default)
    {
        await FeaturedOrderLock.WaitAsync(cancellationToken);
        try
        {
            await using var transaction = dbContext.Database.IsRelational()
                ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
                : null;
            var project = await dbContext.Projects
                .SingleOrDefaultAsync(existingProject => existingProject.Id == projectId, cancellationToken);

            if (project is null)
            {
                return null;
            }

            var wasFeatured = project.IsFeatured;
            project.IsFeatured = isFeatured;

            if (isFeatured)
            {
                if (!wasFeatured || !project.FeaturedOrder.HasValue)
                {
                    var highestOrder = await dbContext.Projects
                        .Where(existing => existing.IsFeatured && existing.FeaturedOrder.HasValue)
                        .MaxAsync(existing => (int?)existing.FeaturedOrder, cancellationToken) ?? -1;
                    project.FeaturedOrder = highestOrder + 1;
                }
            }
            else
            {
                project.FeaturedOrder = null;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return await GetRequiredProjectAsync(projectId, cancellationToken);
        }
        finally
        {
            FeaturedOrderLock.Release();
        }
    }

    public async Task<Project?> SetArchivedStateAsync(
        int projectId,
        bool isArchived,
        CancellationToken cancellationToken = default)
    {
        var project = await dbContext.Projects
            .SingleOrDefaultAsync(existingProject => existingProject.Id == projectId, cancellationToken);

        if (project is null)
        {
            return null;
        }

        if (project.IsArchived == isArchived)
        {
            return await GetRequiredProjectAsync(projectId, cancellationToken);
        }

        project.IsArchived = isArchived;
        project.ArchivedAt = isArchived
            ? DateTimeOffset.UtcNow
            : null;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRequiredProjectAsync(projectId, cancellationToken);
    }

    public async Task<bool> ReorderFeaturedProjectsAsync(
        IReadOnlyCollection<int> orderedFeaturedProjectIds,
        CancellationToken cancellationToken = default)
    {
        await FeaturedOrderLock.WaitAsync(cancellationToken);
        try
        {
            await using var transaction = dbContext.Database.IsRelational()
                ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
                : null;
            var distinctOrderedProjectIds = orderedFeaturedProjectIds
                .Where(projectId => projectId > 0)
                .Distinct()
                .ToList();

            var projectsForReorder = await dbContext.Projects
                .Where(project => distinctOrderedProjectIds.Contains(project.Id))
                .ToListAsync(cancellationToken);

            if (projectsForReorder.Count != distinctOrderedProjectIds.Count ||
                projectsForReorder.Any(project => !project.IsFeatured))
            {
                return false;
            }

            var allFeaturedProjectIds = await dbContext.Projects
                .Where(project => project.IsFeatured)
                .Select(project => project.Id)
                .ToListAsync(cancellationToken);

            if (allFeaturedProjectIds.Count != distinctOrderedProjectIds.Count ||
                allFeaturedProjectIds.Except(distinctOrderedProjectIds).Any())
            {
                return false;
            }

            var currentFeaturedProjects = await dbContext.Projects
                .Where(project => allFeaturedProjectIds.Contains(project.Id))
                .ToListAsync(cancellationToken);

            foreach (var project in currentFeaturedProjects)
            {
                project.FeaturedOrder = null;
            }

            var rankedProjects = distinctOrderedProjectIds
                .Select((projectId, rank) => new { projectId, rank })
                .ToDictionary(item => item.projectId, item => item.rank);

            foreach (var project in projectsForReorder)
            {
                project.FeaturedOrder = rankedProjects[project.Id];
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return true;
        }
        finally
        {
            FeaturedOrderLock.Release();
        }
    }

    private IQueryable<Project> CreateProjectQuery()
    {
        return dbContext.Projects
            .AsNoTracking()
            .Include(project => project.Screenshots)
            .Include(project => project.DeveloperRoles)
            .Include(project => project.ProjectTags)
                .ThenInclude(projectTag => projectTag.Tag)
            .Include(project => project.Collaborators)
                .ThenInclude(collaborator => collaborator.Roles)
            .Include(project => project.Milestones);
    }

    private async Task<Project> GetRequiredProjectAsync(int id, CancellationToken cancellationToken)
    {
        return await CreateProjectQuery()
            .SingleOrDefaultAsync(project => project.Id == id, cancellationToken)
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
