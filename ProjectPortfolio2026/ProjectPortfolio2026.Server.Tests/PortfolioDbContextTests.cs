using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Data;
using ProjectPortfolio2026.Server.Domain.Projects;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class PortfolioDbContextTests
{
    [Test]
    public void DbContext_ExposesConfiguredProjectSets_AndModelMappings()
    {
        using var dbContext = CreateDbContext();

        Assert.Multiple(() =>
        {
            Assert.That(dbContext.Projects, Is.Not.Null);
            Assert.That(dbContext.ProjectCollaborators, Is.Not.Null);
            Assert.That(dbContext.ProjectCollaboratorRoles, Is.Not.Null);
            Assert.That(dbContext.ProjectDeveloperRoles, Is.Not.Null);
            Assert.That(dbContext.ProjectMilestones, Is.Not.Null);
            Assert.That(dbContext.ProjectScreenshots, Is.Not.Null);
            Assert.That(dbContext.ProjectSkills, Is.Not.Null);
            Assert.That(dbContext.ProjectTechnologies, Is.Not.Null);
            Assert.That(dbContext.Model.FindEntityType(typeof(Project))?.GetTableName(), Is.EqualTo("Projects"));
            Assert.That(dbContext.Model.FindEntityType(typeof(ProjectCollaborator))?.GetTableName(), Is.EqualTo("ProjectCollaborators"));
            Assert.That(dbContext.Model.FindEntityType(typeof(ProjectCollaboratorRole))?.GetTableName(), Is.EqualTo("ProjectCollaboratorRoles"));
            Assert.That(dbContext.Model.FindEntityType(typeof(ProjectDeveloperRole))?.GetTableName(), Is.EqualTo("ProjectDeveloperRoles"));
            Assert.That(dbContext.Model.FindEntityType(typeof(ProjectMilestone))?.GetTableName(), Is.EqualTo("ProjectMilestones"));
            Assert.That(dbContext.Model.FindEntityType(typeof(ProjectScreenshot))?.GetTableName(), Is.EqualTo("ProjectScreenshots"));
            Assert.That(dbContext.Model.FindEntityType(typeof(ProjectSkill))?.GetTableName(), Is.EqualTo("ProjectSkills"));
            Assert.That(dbContext.Model.FindEntityType(typeof(ProjectTechnology))?.GetTableName(), Is.EqualTo("ProjectTechnologies"));
        });
    }

    [Test]
    public void DesignTimeFactory_CreatesSqlServerContext_WithAppDataDatabasePath()
    {
        var factory = new DesignTimePortfolioDbContextFactory();

        using var dbContext = factory.CreateDbContext([]);
        var connectionString = dbContext.Database.GetConnectionString();

        Assert.That(connectionString, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(connectionString, Does.Contain("(localdb)\\MSSQLLocalDB"));
            Assert.That(connectionString, Does.Contain("ProjectPortfolio2026DesignTime"));
            Assert.That(connectionString, Does.Contain("App_Data"));
            Assert.That(connectionString, Does.Contain("ProjectPortfolio2026.DesignTime.mdf"));
        });
    }

    private static PortfolioDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new PortfolioDbContext(options);
    }
}
