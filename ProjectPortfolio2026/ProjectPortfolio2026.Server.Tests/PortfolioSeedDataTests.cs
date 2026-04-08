using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Data;
using ProjectPortfolio2026.Server.Data.SeedData;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class PortfolioSeedDataTests
{
    [Test]
    public async Task InitializeAsync_AddsOneHundredProjects_WhenDatabaseIsEmpty()
    {
        await using var dbContext = CreateDbContext();

        await PortfolioSeedData.InitializeAsync(dbContext);

        var projects = await dbContext.Projects
            .Include(project => project.Screenshots)
            .Include(project => project.DeveloperRoles)
            .Include(project => project.ProjectTags)
                .ThenInclude(projectTag => projectTag.Tag)
            .Include(project => project.Collaborators)
                .ThenInclude(collaborator => collaborator.Roles)
            .Include(project => project.Milestones)
            .ToListAsync();

        Assert.That(projects, Has.Count.EqualTo(100));
        Assert.That(projects.All(project => project.Screenshots.Count >= 2), Is.True);
        Assert.That(projects.Single(project => project.Title == "Project Portfolio 2026").Screenshots, Has.Count.EqualTo(6));
        Assert.That(projects.All(project => project.DeveloperRoles.Count > 0), Is.True);
        Assert.That(projects.All(project => project.ProjectTags.Any(projectTag => projectTag.Tag!.Category == ProjectPortfolio2026.Server.Domain.Tags.TagCategory.Technology)), Is.True);
        Assert.That(projects.All(project => project.ProjectTags.Any(projectTag => projectTag.Tag!.Category == ProjectPortfolio2026.Server.Domain.Tags.TagCategory.Skill)), Is.True);
        Assert.That(projects.All(project => project.Milestones.Count > 0), Is.True);
        Assert.That(projects.Count(project => project.EndDate is null), Is.EqualTo(3));
        Assert.That(projects.Min(project => project.StartDate.Year), Is.EqualTo(2015));
        Assert.That(projects.Max(project => (project.EndDate ?? project.StartDate).Year), Is.EqualTo(2026));
        Assert.That(projects.Count(project => project.IsFeatured), Is.EqualTo(5));
    }

    [Test]
    public async Task InitializeAsync_DoesNothing_WhenProjectsAlreadyExist()
    {
        await using var dbContext = CreateDbContext();

        await PortfolioSeedData.InitializeAsync(dbContext);
        await PortfolioSeedData.InitializeAsync(dbContext);

        Assert.That(await dbContext.Projects.CountAsync(), Is.EqualTo(100));
    }

    private static PortfolioDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new PortfolioDbContext(options);
    }
}
