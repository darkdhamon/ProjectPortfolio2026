using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Data;
using ProjectPortfolio2026.Server.Domain.Identity;
using ProjectPortfolio2026.Server.Domain.Portfolio;
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
            Assert.That(dbContext.ApplicationUsers, Is.Not.Null);
            Assert.That(dbContext.PortfolioProfiles, Is.Not.Null);
            Assert.That(dbContext.PortfolioContactMethods, Is.Not.Null);
            Assert.That(dbContext.PortfolioSocialLinks, Is.Not.Null);
            Assert.That(dbContext.Projects, Is.Not.Null);
            Assert.That(dbContext.ProjectCollaborators, Is.Not.Null);
            Assert.That(dbContext.ProjectCollaboratorRoles, Is.Not.Null);
            Assert.That(dbContext.ProjectDeveloperRoles, Is.Not.Null);
            Assert.That(dbContext.ProjectMilestones, Is.Not.Null);
            Assert.That(dbContext.ProjectScreenshots, Is.Not.Null);
            Assert.That(dbContext.ProjectSkills, Is.Not.Null);
            Assert.That(dbContext.ProjectTechnologies, Is.Not.Null);
            Assert.That(dbContext.Model.FindEntityType(typeof(ApplicationUser))?.GetTableName(), Is.EqualTo("AspNetUsers"));
            Assert.That(dbContext.Model.FindEntityType(typeof(IdentityRole))?.GetTableName(), Is.EqualTo("AspNetRoles"));
            Assert.That(dbContext.Model.FindEntityType(typeof(PortfolioProfile))?.GetTableName(), Is.EqualTo("PortfolioProfiles"));
            Assert.That(dbContext.Model.FindEntityType(typeof(PortfolioContactMethod))?.GetTableName(), Is.EqualTo("PortfolioContactMethods"));
            Assert.That(dbContext.Model.FindEntityType(typeof(PortfolioSocialLink))?.GetTableName(), Is.EqualTo("PortfolioSocialLinks"));
            Assert.That(dbContext.Model.FindEntityType(typeof(Project))?.GetTableName(), Is.EqualTo("Projects"));
            Assert.That(dbContext.Model.FindEntityType(typeof(ProjectCollaborator))?.GetTableName(), Is.EqualTo("ProjectCollaborators"));
            Assert.That(dbContext.Model.FindEntityType(typeof(ProjectCollaboratorRole))?.GetTableName(), Is.EqualTo("ProjectCollaboratorRoles"));
            Assert.That(dbContext.Model.FindEntityType(typeof(ProjectDeveloperRole))?.GetTableName(), Is.EqualTo("ProjectDeveloperRoles"));
            Assert.That(dbContext.Model.FindEntityType(typeof(ProjectMilestone))?.GetTableName(), Is.EqualTo("ProjectMilestones"));
            Assert.That(dbContext.Model.FindEntityType(typeof(ProjectScreenshot))?.GetTableName(), Is.EqualTo("ProjectScreenshots"));
            Assert.That(dbContext.Model.FindEntityType(typeof(ProjectSkill))?.GetTableName(), Is.EqualTo("ProjectSkills"));
            Assert.That(dbContext.Model.FindEntityType(typeof(ProjectTechnology))?.GetTableName(), Is.EqualTo("ProjectTechnologies"));
            Assert.That(dbContext.Model.FindEntityType(typeof(PortfolioProfile))?.FindProperty(nameof(PortfolioProfile.DisplayName))?.GetMaxLength(), Is.EqualTo(200));
            Assert.That(dbContext.Model.FindEntityType(typeof(PortfolioContactMethod))?.FindProperty(nameof(PortfolioContactMethod.Value))?.GetMaxLength(), Is.EqualTo(250));
            Assert.That(dbContext.Model.FindEntityType(typeof(PortfolioSocialLink))?.FindProperty(nameof(PortfolioSocialLink.Url))?.GetMaxLength(), Is.EqualTo(500));
            Assert.That(dbContext.Model.FindEntityType(typeof(ApplicationUser))?.FindProperty(nameof(ApplicationUser.DisplayName))?.GetMaxLength(), Is.EqualTo(256));
            Assert.That(dbContext.Model.FindEntityType(typeof(ApplicationUser))?.GetIndexes().Any(index =>
                index.Properties.Select(property => property.Name).SequenceEqual([nameof(ApplicationUser.NormalizedEmail)]) &&
                index.IsUnique), Is.True);
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

    [Test]
    public void LocalDbRecovery_ExtractsDatabaseNameAndAttachPath()
    {
        const string connectionString = "Server=(localdb)\\MSSQLLocalDB;AttachDbFilename=C:\\Temp\\ProjectPortfolio2026.Dev.mdf;Database=ProjectPortfolio2026Dev;Integrated Security=True";

        var result = LocalDbDatabaseRecovery.TryGetRecoveryTarget(connectionString, out var target);

        Assert.That(result, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(target.DatabaseName, Is.EqualTo("ProjectPortfolio2026Dev"));
            Assert.That(target.AttachDbFilePath, Is.EqualTo("C:\\Temp\\ProjectPortfolio2026.Dev.mdf"));
        });
    }

    [Test]
    public void LocalDbRecovery_DoesNotTargetNonLocalDbConnections()
    {
        const string connectionString = "Server=.\\SQLEXPRESS;AttachDbFilename=C:\\Temp\\ProjectPortfolio2026.Dev.mdf;Database=ProjectPortfolio2026Dev;Integrated Security=True";

        var result = LocalDbDatabaseRecovery.TryGetRecoveryTarget(connectionString, out var target);

        Assert.That(result, Is.False);
        Assert.That(target, Is.EqualTo(default(LocalDbRecoveryTarget)));
    }

    [Test]
    public void LocalDbRecovery_CanRecoverOnlyWhenAttachFileIsMissingAndErrorMatches()
    {
        var missingFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.mdf");
        var existingFile = Path.GetTempFileName();

        try
        {
            var missingConnectionString = $"Server=(localdb)\\MSSQLLocalDB;AttachDbFilename={missingFile};Database=ProjectPortfolio2026Dev;Integrated Security=True";
            var existingConnectionString = $"Server=(localdb)\\MSSQLLocalDB;AttachDbFilename={existingFile};Database=ProjectPortfolio2026Dev;Integrated Security=True";

            Assert.Multiple(() =>
            {
                Assert.That(LocalDbDatabaseRecovery.CanRecover(missingConnectionString, 1801, "Database 'ProjectPortfolio2026Dev' already exists."), Is.True);
                Assert.That(LocalDbDatabaseRecovery.CanRecover(existingConnectionString, 1801, "Database 'ProjectPortfolio2026Dev' already exists."), Is.False);
                Assert.That(LocalDbDatabaseRecovery.CanRecover(missingConnectionString, 4060, "Cannot open database requested by the login."), Is.False);
                Assert.That(LocalDbDatabaseRecovery.CanRecover($"Server=.\\SQLEXPRESS;AttachDbFilename={missingFile};Database=ProjectPortfolio2026Dev;Integrated Security=True", 1801, "Database 'ProjectPortfolio2026Dev' already exists."), Is.False);
            });
        }
        finally
        {
            File.Delete(existingFile);
        }
    }

    [Test]
    public void LocalDbRecovery_CreatesAttachDirectory_WhenAttachPathHasMissingFolders()
    {
        var attachDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "App_Data");
        var missingFile = Path.Combine(attachDirectory, "ProjectPortfolio2026.Dev.mdf");
        var rootDirectory = Path.GetDirectoryName(attachDirectory)!;

        try
        {
            if (Directory.Exists(attachDirectory))
            {
                Directory.Delete(attachDirectory, recursive: true);
            }

            LocalDbDatabaseRecovery.EnsureAttachDirectoryExists(missingFile);

            Assert.That(Directory.Exists(attachDirectory), Is.True);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Test]
    public void ConnectionStringPathResolver_ResolvesRelativeAttachPathAgainstContentRoot()
    {
        var contentRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        const string connectionString = "Server=(localdb)\\MSSQLLocalDB;AttachDbFilename=App_Data\\ProjectPortfolio2026.Dev.mdf;Database=ProjectPortfolio2026Dev;Integrated Security=True";

        try
        {
            var resolvedConnectionString = ConnectionStringPathResolver.ResolveDataPaths(connectionString, contentRootPath);
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(resolvedConnectionString);

            Assert.Multiple(() =>
            {
                Assert.That(Path.IsPathRooted(builder.AttachDBFilename), Is.True);
                Assert.That(builder.AttachDBFilename, Is.EqualTo(Path.Combine(contentRootPath, "App_Data", "ProjectPortfolio2026.Dev.mdf")));
                Assert.That(Directory.Exists(Path.Combine(contentRootPath, "App_Data")), Is.True);
            });
        }
        finally
        {
            if (Directory.Exists(contentRootPath))
            {
                Directory.Delete(contentRootPath, recursive: true);
            }
        }
    }

    [Test]
    public void ConnectionStringPathResolver_ReplacesDataDirectoryToken()
    {
        var contentRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        const string connectionString = "Server=(localdb)\\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\\ProjectPortfolio2026.Dev.mdf;Database=ProjectPortfolio2026Dev;Integrated Security=True";

        try
        {
            var resolvedConnectionString = ConnectionStringPathResolver.ResolveDataPaths(connectionString, contentRootPath);
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(resolvedConnectionString);

            Assert.That(builder.AttachDBFilename, Is.EqualTo(Path.Combine(contentRootPath, "App_Data", "ProjectPortfolio2026.Dev.mdf")));
        }
        finally
        {
            if (Directory.Exists(contentRootPath))
            {
                Directory.Delete(contentRootPath, recursive: true);
            }
        }
    }

    private static PortfolioDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new PortfolioDbContext(options);
    }
}
