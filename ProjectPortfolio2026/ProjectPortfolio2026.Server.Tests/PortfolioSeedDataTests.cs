using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Data;
using ProjectPortfolio2026.Server.Data.SeedData;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class PortfolioSeedDataTests
{
    [Test]
    public async Task InitializeAsync_AddsTenProjects_WhenDatabaseIsEmpty()
    {
        await using var dbContext = CreateDbContext();

        await PortfolioSeedData.InitializeAsync(dbContext);

        var projects = await dbContext.Projects
            .Include(project => project.Screenshots)
            .Include(project => project.DeveloperRoles)
            .Include(project => project.Technologies)
            .Include(project => project.Skills)
            .Include(project => project.Collaborators)
                .ThenInclude(collaborator => collaborator.Roles)
            .Include(project => project.Milestones)
            .ToListAsync();

        Assert.That(projects, Has.Count.EqualTo(10));
        Assert.That(projects.All(project => project.Screenshots.Count == 2), Is.True);
        Assert.That(projects.All(project => project.DeveloperRoles.Count > 0), Is.True);
        Assert.That(projects.All(project => project.Technologies.Count > 0), Is.True);
        Assert.That(projects.All(project => project.Skills.Count > 0), Is.True);
        Assert.That(projects.All(project => project.Milestones.Count > 0), Is.True);
        Assert.That(projects.Count(project => project.IsFeatured), Is.EqualTo(5));
    }

    [Test]
    public async Task InitializeAsync_DoesNothing_WhenProjectsAlreadyExist()
    {
        await using var dbContext = CreateDbContext();

        await PortfolioSeedData.InitializeAsync(dbContext);
        await PortfolioSeedData.InitializeAsync(dbContext);

        Assert.That(await dbContext.Projects.CountAsync(), Is.EqualTo(10));
    }

    private static PortfolioDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new PortfolioDbContext(options);
    }
}
