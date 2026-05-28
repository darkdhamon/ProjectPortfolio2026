using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Data;
using ProjectPortfolio2026.Server.Domain.Portfolio;
using ProjectPortfolio2026.Server.Repositories;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class ResumeConfigurationRepositoryTests
{
    [Test]
    public async Task GetAsync_ReturnsNewestConfiguration()
    {
        await using var dbContext = CreateDbContext();
        dbContext.ResumeConfigurations.AddRange(
            new ResumeConfiguration
            {
                SourceType = ResumeSourceTypes.HostedFile,
                SourceUrl = "https://cdn.example.dev/resume-v1.pdf",
                DisplayLabel = "Download Resume"
            },
            new ResumeConfiguration
            {
                SourceType = ResumeSourceTypes.Embed,
                SourceUrl = "https://drive.example.dev/embed/resume",
                DisplayLabel = "Open Embedded Resume"
            });
        await dbContext.SaveChangesAsync();

        var repository = new ResumeConfigurationRepository(dbContext);
        var configuration = await repository.GetAsync();

        Assert.That(configuration, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(configuration!.SourceType, Is.EqualTo(ResumeSourceTypes.Embed));
            Assert.That(configuration.SourceUrl, Is.EqualTo("https://drive.example.dev/embed/resume"));
            Assert.That(configuration.DisplayLabel, Is.EqualTo("Open Embedded Resume"));
        });
    }

    [Test]
    public async Task SaveAsync_UpdatesExistingSingletonRecord()
    {
        await using var dbContext = CreateDbContext();
        dbContext.ResumeConfigurations.Add(new ResumeConfiguration
        {
            SourceType = ResumeSourceTypes.HostedFile,
            SourceUrl = "https://cdn.example.dev/resume-v1.pdf",
            DisplayLabel = "Download Resume",
            Summary = "Original summary"
        });
        await dbContext.SaveChangesAsync();

        var repository = new ResumeConfigurationRepository(dbContext);
        var savedConfiguration = await repository.SaveAsync(new ResumeConfiguration
        {
            SourceType = ResumeSourceTypes.Embed,
            SourceUrl = "https://drive.example.dev/embed/resume",
            DisplayLabel = "Open Embedded Resume",
            Summary = "Updated summary"
        });

        var persistedConfigurations = await dbContext.ResumeConfigurations.OrderBy(configuration => configuration.Id).ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(savedConfiguration.Id, Is.GreaterThan(0));
            Assert.That(persistedConfigurations, Has.Count.EqualTo(1));
            Assert.That(persistedConfigurations[0].SourceType, Is.EqualTo(ResumeSourceTypes.Embed));
            Assert.That(persistedConfigurations[0].DisplayLabel, Is.EqualTo("Open Embedded Resume"));
            Assert.That(persistedConfigurations[0].Summary, Is.EqualTo("Updated summary"));
        });
    }

    [Test]
    public async Task SaveAsync_CreatesFirstConfigurationWhenDatabaseIsEmpty()
    {
        await using var dbContext = CreateDbContext();
        var repository = new ResumeConfigurationRepository(dbContext);

        var savedConfiguration = await repository.SaveAsync(new ResumeConfiguration
        {
            SourceType = ResumeSourceTypes.HostedFile,
            SourceUrl = "https://cdn.example.dev/resume-v1.pdf",
            DisplayLabel = "Download Resume",
            Summary = "Initial summary"
        });

        var persistedConfigurations = await dbContext.ResumeConfigurations.ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(savedConfiguration.Id, Is.GreaterThan(0));
            Assert.That(persistedConfigurations, Has.Count.EqualTo(1));
            Assert.That(persistedConfigurations[0].DisplayLabel, Is.EqualTo("Download Resume"));
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
