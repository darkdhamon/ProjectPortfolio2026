using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Data;
using ProjectPortfolio2026.Server.Domain.Projects;
using ProjectPortfolio2026.Server.Repositories;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class ProjectRepositoryTests
{
    [Test]
    public async Task AddAsync_PersistsProjectGraph_WithDefaultFlagsDisabled()
    {
        await using var dbContext = CreateDbContext();
        var repository = new ProjectRepository(dbContext);

        var project = new Project
        {
            Title = "Portfolio Platform",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "Developer portfolio for recruiters.",
            LongDescriptionMarkdown = "Long form description.",
            DeveloperRoles = [new ProjectDeveloperRole { Name = "Backend" }],
            Technologies = [new ProjectTechnology { Name = ".NET" }],
            Skills = [new ProjectSkill { Name = "API Design" }],
            Screenshots = [new ProjectScreenshot { ImageUrl = "https://example.test/hero.png", SortOrder = 1 }],
            Collaborators =
            [
                new ProjectCollaborator
                {
                    Name = "Teammate",
                    Roles = [new ProjectCollaboratorRole { Name = "Designer" }]
                }
            ],
            Milestones =
            [
                new ProjectMilestone
                {
                    Title = "MVP",
                    TargetDate = new DateOnly(2026, 5, 1)
                }
            ]
        };

        var savedProject = await repository.AddAsync(project);

        Assert.Multiple(() =>
        {
            Assert.That(savedProject.Id, Is.GreaterThan(0));
            Assert.That(savedProject.IsPublished, Is.False);
            Assert.That(savedProject.IsFeatured, Is.False);
            Assert.That(savedProject.DeveloperRoles.Select(role => role.Name), Is.EquivalentTo(new[] { "Backend" }));
            Assert.That(savedProject.Technologies.Select(technology => technology.Name), Is.EquivalentTo(new[] { ".NET" }));
            Assert.That(savedProject.Skills.Select(skill => skill.Name), Is.EquivalentTo(new[] { "API Design" }));
            Assert.That(savedProject.Collaborators, Has.Count.EqualTo(1));
            Assert.That(savedProject.Collaborators[0].Roles.Select(role => role.Name), Is.EquivalentTo(new[] { "Designer" }));
            Assert.That(savedProject.Milestones.Select(milestone => milestone.Title), Is.EquivalentTo(new[] { "MVP" }));
        });
    }

    [Test]
    public async Task UpdateAsync_ReplacesNestedCollections()
    {
        await using var dbContext = CreateDbContext();
        var repository = new ProjectRepository(dbContext);

        var project = await repository.AddAsync(new Project
        {
            Title = "Portfolio Platform",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "Initial description.",
            LongDescriptionMarkdown = "Initial markdown.",
            Skills = [new ProjectSkill { Name = "React" }]
        });

        project.Title = "Portfolio Platform v2";
        project.ShortDescription = "Updated description.";
        project.IsPublished = true;
        project.IsFeatured = true;
        project.Skills = [new ProjectSkill { Name = "Entity Framework" }];
        project.Technologies = [new ProjectTechnology { Name = "SQL Server" }];
        project.DeveloperRoles = [new ProjectDeveloperRole { Name = "Full Stack" }];

        var updatedProject = await repository.UpdateAsync(project);

        Assert.That(updatedProject, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(updatedProject!.Title, Is.EqualTo("Portfolio Platform v2"));
            Assert.That(updatedProject.ShortDescription, Is.EqualTo("Updated description."));
            Assert.That(updatedProject.IsPublished, Is.True);
            Assert.That(updatedProject.IsFeatured, Is.True);
            Assert.That(updatedProject.Skills.Select(skill => skill.Name), Is.EquivalentTo(new[] { "Entity Framework" }));
            Assert.That(updatedProject.Technologies.Select(technology => technology.Name), Is.EquivalentTo(new[] { "SQL Server" }));
            Assert.That(updatedProject.DeveloperRoles.Select(role => role.Name), Is.EquivalentTo(new[] { "Full Stack" }));
        });
    }

    [Test]
    public async Task ListAsync_ReturnsPublishedProjectsMatchingSearchAndSkillFilters()
    {
        await using var dbContext = CreateDbContext();
        var repository = new ProjectRepository(dbContext);

        await repository.AddAsync(new Project
        {
            Title = "Portfolio Platform",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "Searchable app platform.",
            LongDescriptionMarkdown = "Builds polished project portfolios.",
            IsPublished = true,
            Skills =
            [
                new ProjectSkill { Name = "API Design" },
                new ProjectSkill { Name = "React" }
            ],
            Technologies = [new ProjectTechnology { Name = "SQL Server" }]
        });

        await repository.AddAsync(new Project
        {
            Title = "Internal Draft",
            StartDate = new DateOnly(2026, 5, 1),
            ShortDescription = "Should not appear in public list.",
            LongDescriptionMarkdown = "Unpublished work.",
            IsPublished = false,
            Skills = [new ProjectSkill { Name = "React" }]
        });

        await repository.AddAsync(new Project
        {
            Title = "Analytics Dashboard",
            StartDate = new DateOnly(2026, 3, 1),
            ShortDescription = "Visualization tools.",
            LongDescriptionMarkdown = "Focused on insights.",
            IsPublished = true,
            Skills = [new ProjectSkill { Name = "Data Visualization" }]
        });

        var page = await repository.ListAsync(
            "portfolio",
            ["react", "api design"],
            1,
            6);

        Assert.Multiple(() =>
        {
            Assert.That(page.TotalCount, Is.EqualTo(1));
            Assert.That(page.Items, Has.Count.EqualTo(1));
            Assert.That(page.Items[0].Title, Is.EqualTo("Portfolio Platform"));
            Assert.That(page.Items[0].Skills, Is.EquivalentTo(new[] { "API Design", "React" }));
            Assert.That(page.AvailableSkills, Is.EquivalentTo(new[] { "API Design", "Data Visualization", "React" }));
        });
    }

    [Test]
    public async Task ListAsync_AppliesPagingAndNormalizesInputs()
    {
        await using var dbContext = CreateDbContext();
        var repository = new ProjectRepository(dbContext);

        for (var index = 1; index <= 8; index++)
        {
            await repository.AddAsync(new Project
            {
                Title = $"Project {index:00}",
                StartDate = new DateOnly(2026, index <= 6 ? index : 6, 1),
                ShortDescription = $"Summary {index}",
                LongDescriptionMarkdown = $"Markdown {index}",
                IsPublished = true,
                Skills = [new ProjectSkill { Name = index % 2 == 0 ? "React" : "C#" }]
            });
        }

        var page = await repository.ListAsync(
            "   ",
            ["React", "react", ""],
            0,
            100);

        Assert.Multiple(() =>
        {
            Assert.That(page.Page, Is.EqualTo(1));
            Assert.That(page.PageSize, Is.EqualTo(50));
            Assert.That(page.TotalCount, Is.EqualTo(4));
            Assert.That(page.Items, Has.Count.EqualTo(4));
            Assert.That(page.HasMore, Is.False);
            Assert.That(page.Items.Select(item => item.Title), Is.EqualTo(new[] { "Project 06", "Project 08", "Project 04", "Project 02" }));
        });
    }

    [Test]
    public async Task ListFeaturedAsync_ReturnsAtMostFiveFeaturedProjects_WhenEnoughFeaturedProjectsExist()
    {
        await using var dbContext = CreateDbContext();
        var repository = new ProjectRepository(dbContext);

        for (var index = 1; index <= 7; index += 1)
        {
            await repository.AddAsync(new Project
            {
                Title = $"Featured Project {index}",
                StartDate = new DateOnly(2026, index, 1),
                ShortDescription = $"Featured summary {index}",
                LongDescriptionMarkdown = $"Featured markdown {index}",
                IsPublished = true,
                IsFeatured = true
            });
        }

        var projects = await repository.ListFeaturedAsync(5);

        Assert.Multiple(() =>
        {
            Assert.That(projects, Has.Count.EqualTo(5));
            Assert.That(projects.All(project => project.IsFeatured), Is.True);
            Assert.That(projects.Select(project => project.Id).Distinct().Count(), Is.EqualTo(5));
        });
    }

    [Test]
    public async Task ListFeaturedAsync_FillsRemainingSlotsWithMostRecentPublishedProjects_WhenFeaturedProjectsAreLimited()
    {
        await using var dbContext = CreateDbContext();
        var repository = new ProjectRepository(dbContext);

        await repository.AddAsync(new Project
        {
            Title = "Featured Alpha",
            StartDate = new DateOnly(2026, 1, 1),
            ShortDescription = "Featured summary",
            LongDescriptionMarkdown = "Featured markdown",
            IsPublished = true,
            IsFeatured = true
        });

        await repository.AddAsync(new Project
        {
            Title = "Recent Gamma",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "Recent summary",
            LongDescriptionMarkdown = "Recent markdown",
            IsPublished = true
        });

        await repository.AddAsync(new Project
        {
            Title = "Recent Beta",
            StartDate = new DateOnly(2026, 3, 1),
            ShortDescription = "Recent summary",
            LongDescriptionMarkdown = "Recent markdown",
            IsPublished = true
        });

        await repository.AddAsync(new Project
        {
            Title = "Recent Delta",
            StartDate = new DateOnly(2026, 2, 1),
            ShortDescription = "Recent summary",
            LongDescriptionMarkdown = "Recent markdown",
            IsPublished = true
        });

        await repository.AddAsync(new Project
        {
            Title = "Recent Epsilon",
            StartDate = new DateOnly(2025, 12, 1),
            ShortDescription = "Recent summary",
            LongDescriptionMarkdown = "Recent markdown",
            IsPublished = true
        });

        await repository.AddAsync(new Project
        {
            Title = "Hidden Draft",
            StartDate = new DateOnly(2026, 5, 1),
            ShortDescription = "Draft summary",
            LongDescriptionMarkdown = "Draft markdown",
            IsPublished = false,
            IsFeatured = true
        });

        var projects = await repository.ListFeaturedAsync(5);

        Assert.That(projects.Select(project => project.Title), Is.EqualTo(new[]
        {
            "Featured Alpha",
            "Recent Gamma",
            "Recent Beta",
            "Recent Delta",
            "Recent Epsilon"
        }));
    }

    private static PortfolioDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new PortfolioDbContext(options);
    }
}
