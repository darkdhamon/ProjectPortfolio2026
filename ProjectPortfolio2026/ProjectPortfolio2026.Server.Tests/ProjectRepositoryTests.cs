using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Data;
using ProjectPortfolio2026.Server.Domain.Projects;
using ProjectPortfolio2026.Server.Domain.Tags;
using ProjectPortfolio2026.Server.Repositories;
using ProjectPortfolio2026.Server.Services.Implementations;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class ProjectRepositoryTests
{
    [Test]
    public async Task AddAsync_PersistsProjectGraph_WithDefaultFlagsDisabled()
    {
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

        var project = new Project
        {
            Title = "Portfolio Platform",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "Developer portfolio for recruiters.",
            LongDescriptionMarkdown = "Long form description.",
            DeveloperRoles = [new ProjectDeveloperRole { Name = "Backend" }],
            ProjectTags =
            [
                CreateProjectTag(TagCategory.Technology, ".NET"),
                CreateProjectTag(TagCategory.Skill, "API Design")
            ],
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
            Assert.That(savedProject.IsArchived, Is.False);
            Assert.That(savedProject.ArchivedAt, Is.Null);
            Assert.That(savedProject.DeveloperRoles.Select(role => role.Name), Is.EquivalentTo(new[] { "Backend" }));
            Assert.That(savedProject.ProjectTags.Where(projectTag => projectTag.Tag!.Category == TagCategory.Technology).Select(projectTag => projectTag.Tag!.DisplayName), Is.EquivalentTo(new[] { ".NET" }));
            Assert.That(savedProject.ProjectTags.Where(projectTag => projectTag.Tag!.Category == TagCategory.Skill).Select(projectTag => projectTag.Tag!.DisplayName), Is.EquivalentTo(new[] { "API Design" }));
            Assert.That(savedProject.Collaborators, Has.Count.EqualTo(1));
            Assert.That(savedProject.Collaborators[0].Roles.Select(role => role.Name), Is.EquivalentTo(new[] { "Designer" }));
            Assert.That(savedProject.Milestones.Select(milestone => milestone.Title), Is.EquivalentTo(new[] { "MVP" }));
        });
    }

    [Test]
    public async Task UpdateAsync_ReplacesNestedCollections()
    {
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

        var project = await repository.AddAsync(new Project
        {
            Title = "Portfolio Platform",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "Initial description.",
            LongDescriptionMarkdown = "Initial markdown.",
            ProjectTags = [CreateProjectTag(TagCategory.Skill, "React")]
        });

        project.Title = "Portfolio Platform v2";
        project.ShortDescription = "Updated description.";
        project.IsPublished = true;
        project.IsFeatured = true;
        project.ProjectTags =
        [
            CreateProjectTag(TagCategory.Skill, "Entity Framework"),
            CreateProjectTag(TagCategory.Technology, "SQL Server")
        ];
        project.DeveloperRoles = [new ProjectDeveloperRole { Name = "Full Stack" }];

        var updatedProject = await repository.UpdateAsync(project);

        Assert.That(updatedProject, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(updatedProject!.Title, Is.EqualTo("Portfolio Platform v2"));
            Assert.That(updatedProject.ShortDescription, Is.EqualTo("Updated description."));
            Assert.That(updatedProject.IsPublished, Is.True);
            Assert.That(updatedProject.IsFeatured, Is.True);
            Assert.That(updatedProject.ProjectTags.Where(projectTag => projectTag.Tag!.Category == TagCategory.Skill).Select(projectTag => projectTag.Tag!.DisplayName), Is.EquivalentTo(new[] { "Entity Framework" }));
            Assert.That(updatedProject.ProjectTags.Where(projectTag => projectTag.Tag!.Category == TagCategory.Technology).Select(projectTag => projectTag.Tag!.DisplayName), Is.EquivalentTo(new[] { "SQL Server" }));
            Assert.That(updatedProject.DeveloperRoles.Select(role => role.Name), Is.EquivalentTo(new[] { "Full Stack" }));
        });
    }

    [Test]
    public async Task AddAsync_DeduplicatesProjectTagsByNormalizedName()
    {
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

        var project = new Project
        {
            Title = "Normalization Check",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "Checks duplicate tag normalization.",
            LongDescriptionMarkdown = "Normalization details.",
            ProjectTags =
            [
                CreateProjectTag(TagCategory.Skill, "React"),
                CreateProjectTag(TagCategory.Skill, " react "),
                CreateProjectTag(TagCategory.Technology, ".NET"),
                CreateProjectTag(TagCategory.Technology, " .net ")
            ]
        };

        var savedProject = await repository.AddAsync(project);
        var savedSkillTags = savedProject.ProjectTags
            .Where(projectTag => projectTag.Tag!.Category == TagCategory.Skill)
            .ToList();
        var savedTechnologyTags = savedProject.ProjectTags
            .Where(projectTag => projectTag.Tag!.Category == TagCategory.Technology)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(savedSkillTags, Has.Count.EqualTo(1));
            Assert.That(savedTechnologyTags, Has.Count.EqualTo(1));
            Assert.That(savedProject.ProjectTags.Select(projectTag => projectTag.Tag!.NormalizedName), Is.EquivalentTo(new[] { "REACT", ".NET" }));
        });
    }

    [Test]
    public async Task ListAsync_ReturnsPublishedProjectsMatchingSearchAndSkillFilters()
    {
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

        await repository.AddAsync(new Project
        {
            Title = "Portfolio Platform",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "Searchable app platform.",
            LongDescriptionMarkdown = "Builds polished project portfolios.",
            IsPublished = true,
            ProjectTags =
            [
                CreateProjectTag(TagCategory.Skill, "API Design"),
                CreateProjectTag(TagCategory.Skill, "React"),
                CreateProjectTag(TagCategory.Technology, "SQL Server")
            ]
        });

        await repository.AddAsync(new Project
        {
            Title = "Internal Draft",
            StartDate = new DateOnly(2026, 5, 1),
            ShortDescription = "Should not appear in public list.",
            LongDescriptionMarkdown = "Unpublished work.",
            IsPublished = false,
            ProjectTags = [CreateProjectTag(TagCategory.Skill, "React")]
        });

        await repository.AddAsync(new Project
        {
            Title = "Analytics Dashboard",
            StartDate = new DateOnly(2026, 3, 1),
            ShortDescription = "Visualization tools.",
            LongDescriptionMarkdown = "Focused on insights.",
            IsPublished = true,
            ProjectTags = [CreateProjectTag(TagCategory.Skill, "Data Visualization")]
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
        var repository = CreateRepository(dbContext);

        for (var index = 1; index <= 8; index++)
        {
            await repository.AddAsync(new Project
            {
                Title = $"Project {index:00}",
                StartDate = new DateOnly(2026, index <= 6 ? index : 6, 1),
                ShortDescription = $"Summary {index}",
                LongDescriptionMarkdown = $"Markdown {index}",
                IsPublished = true,
                ProjectTags = [CreateProjectTag(TagCategory.Skill, index % 2 == 0 ? "React" : "C#")]
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
    public async Task ListAsync_ExcludesArchivedProjectsFromResultAndAvailableSkills()
    {
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

        await repository.AddAsync(new Project
        {
            Title = "Archived Project",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "Historical project.",
            LongDescriptionMarkdown = "Archived content.",
            IsPublished = true,
            IsArchived = true,
            ProjectTags = [CreateProjectTag(TagCategory.Skill, "React")]
        });

        await repository.AddAsync(new Project
        {
            Title = "Active Project",
            StartDate = new DateOnly(2026, 3, 1),
            ShortDescription = "Live project.",
            LongDescriptionMarkdown = "Available to public.",
            IsPublished = true,
            IsArchived = false,
            ProjectTags = [CreateProjectTag(TagCategory.Skill, "Blazor")]
        });

        var page = await repository.ListAsync(null, [], 1, 10);

        Assert.Multiple(() =>
        {
            Assert.That(page.TotalCount, Is.EqualTo(1));
            Assert.That(page.Items, Has.Count.EqualTo(1));
            Assert.That(page.Items[0].Title, Is.EqualTo("Active Project"));
            Assert.That(page.AvailableSkills, Is.EqualTo(new[] { "Blazor" }));
        });
    }

    [Test]
    public async Task ListAdminAsync_IncludesArchivedProjectsInResult()
    {
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

        await repository.AddAsync(new Project
        {
            Title = "Archived Project",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "Historical project.",
            LongDescriptionMarkdown = "Archived content.",
            IsPublished = true,
            IsArchived = true,
            ProjectTags = [CreateProjectTag(TagCategory.Skill, "React")]
        });

        await repository.AddAsync(new Project
        {
            Title = "Active Project",
            StartDate = new DateOnly(2026, 3, 1),
            ShortDescription = "Live project.",
            LongDescriptionMarkdown = "Available to public.",
            IsPublished = true,
            IsArchived = false,
            ProjectTags = [CreateProjectTag(TagCategory.Skill, "Blazor")]
        });

        var page = await repository.ListAdminAsync(null, [], 1, 10);

        Assert.Multiple(() =>
        {
            Assert.That(page.TotalCount, Is.EqualTo(2));
            Assert.That(page.Items, Has.Count.EqualTo(2));
            Assert.That(page.Items.Select(item => item.IsArchived), Is.EquivalentTo(new[] { true, false }));
        });
    }

    [Test]
    public async Task ListFeaturedAsync_ReturnsAtMostFiveFeaturedProjects_WhenEnoughFeaturedProjectsExist()
    {
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

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
        var repository = CreateRepository(dbContext);

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

    [Test]
    public async Task ListFeaturedAsync_UsesExplicitFeaturedOrderWhenAvailable()
    {
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

        await repository.AddAsync(new Project
        {
            Title = "Featured C",
            StartDate = new DateOnly(2026, 1, 1),
            ShortDescription = "First with order 2.",
            LongDescriptionMarkdown = "Markdown.",
            IsPublished = true,
            IsFeatured = true,
            FeaturedOrder = 2
        });

        await repository.AddAsync(new Project
        {
            Title = "Featured A",
            StartDate = new DateOnly(2026, 3, 1),
            ShortDescription = "Leading with order 0.",
            LongDescriptionMarkdown = "Markdown.",
            IsPublished = true,
            IsFeatured = true,
            FeaturedOrder = 0
        });

        await repository.AddAsync(new Project
        {
            Title = "Featured B",
            StartDate = new DateOnly(2026, 2, 1),
            ShortDescription = "Middle with order 1.",
            LongDescriptionMarkdown = "Markdown.",
            IsPublished = true,
            IsFeatured = true,
            FeaturedOrder = 1
        });

        await repository.AddAsync(new Project
        {
            Title = "Featured Legacy",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "No explicit order.",
            LongDescriptionMarkdown = "Markdown.",
            IsPublished = true,
            IsFeatured = true
        });

        var projects = await repository.ListFeaturedAsync(5);

        Assert.That(projects.Select(project => project.Title), Is.EqualTo(new[]
        {
            "Featured A",
            "Featured B",
            "Featured C",
            "Featured Legacy"
        }));
    }

    [Test]
    public async Task ListFeaturedAsync_ExcludesArchivedProjectsFromFeaturedResults()
    {
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

        await repository.AddAsync(new Project
        {
            Title = "Archived Featured",
            StartDate = new DateOnly(2026, 1, 1),
            ShortDescription = "Archived and featured.",
            LongDescriptionMarkdown = "Archived content.",
            IsPublished = true,
            IsFeatured = true,
            IsArchived = true
        });

        await repository.AddAsync(new Project
        {
            Title = "Public Project",
            StartDate = new DateOnly(2026, 2, 1),
            ShortDescription = "Public project.",
            LongDescriptionMarkdown = "Available.",
            IsPublished = true,
            IsFeatured = true,
            IsArchived = false
        });

        var projects = await repository.ListFeaturedAsync(5);

        Assert.That(projects.Select(project => project.Title), Is.EqualTo(new[]
        {
            "Public Project"
        }));
    }

    [Test]
    public async Task UpdateFeaturedStateAsync_AssignsOrderForFeaturedProjectsAndClearsForUnfeaturedProjects()
    {
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

        var firstProject = await repository.AddAsync(new Project
        {
            Title = "Project A",
            StartDate = new DateOnly(2026, 2, 1),
            ShortDescription = "Draft project.",
            LongDescriptionMarkdown = "Markdown.",
            IsPublished = true
        });

        var secondProject = await repository.AddAsync(new Project
        {
            Title = "Project B",
            StartDate = new DateOnly(2026, 2, 1),
            ShortDescription = "Published project.",
            LongDescriptionMarkdown = "Markdown.",
            IsPublished = true
        });

        var firstFeatured = await repository.UpdateFeaturedStateAsync(firstProject.Id, true);
        var secondFeatured = await repository.UpdateFeaturedStateAsync(secondProject.Id, true);

        Assert.That(firstFeatured, Is.Not.Null);
        Assert.That(secondFeatured, Is.Not.Null);
        Assert.That(firstFeatured!.FeaturedOrder, Is.EqualTo(0));
        Assert.That(secondFeatured!.FeaturedOrder, Is.EqualTo(1));

        var unfeatured = await repository.UpdateFeaturedStateAsync(firstProject.Id, false);
        Assert.That(unfeatured, Is.Not.Null);
        Assert.That(unfeatured!.IsFeatured, Is.False);
        Assert.That(unfeatured.FeaturedOrder, Is.Null);
    }

    [Test]
    public async Task UpdateFeaturedStateAsync_ReturnsNull_WhenProjectDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

        var updatedProject = await repository.UpdateFeaturedStateAsync(999, true);

        Assert.That(updatedProject, Is.Null);
    }

    [Test]
    public async Task ReorderFeaturedProjectsAsync_ReordersFeaturedProjectsAndReturnsTrue()
    {
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

        var firstProject = await repository.AddAsync(new Project
        {
            Title = "Alpha",
            StartDate = new DateOnly(2026, 1, 1),
            ShortDescription = "Featured alpha.",
            LongDescriptionMarkdown = "Markdown.",
            IsPublished = true,
            IsFeatured = true
        });

        var secondProject = await repository.AddAsync(new Project
        {
            Title = "Beta",
            StartDate = new DateOnly(2026, 2, 1),
            ShortDescription = "Featured beta.",
            LongDescriptionMarkdown = "Markdown.",
            IsPublished = true,
            IsFeatured = true
        });

        var thirdProject = await repository.AddAsync(new Project
        {
            Title = "Gamma",
            StartDate = new DateOnly(2026, 3, 1),
            ShortDescription = "Featured gamma.",
            LongDescriptionMarkdown = "Markdown.",
            IsPublished = true,
            IsFeatured = true
        });

        var isUpdated = await repository.ReorderFeaturedProjectsAsync([
            thirdProject.Id,
            firstProject.Id,
            secondProject.Id
        ]);

        var reorderedFeatured = await repository.ListFeaturedAsync(5);
        var featuredOrders = reorderedFeatured
            .ToDictionary(project => project.Title, project => project.FeaturedOrder);

        Assert.That(isUpdated, Is.True);
        Assert.That(featuredOrders["Gamma"], Is.EqualTo(0));
        Assert.That(featuredOrders["Alpha"], Is.EqualTo(1));
        Assert.That(featuredOrders["Beta"], Is.EqualTo(2));
        Assert.That(reorderedFeatured.All(project => project.IsFeatured), Is.True);
    }

    [Test]
    public async Task ReorderFeaturedProjectsAsync_IgnoresNonPositiveProjectIds()
    {
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

        var featuredProject = await repository.AddAsync(new Project
        {
            Title = "Alpha",
            StartDate = new DateOnly(2026, 1, 1),
            ShortDescription = "Featured alpha.",
            LongDescriptionMarkdown = "Markdown.",
            IsPublished = true,
            IsFeatured = true
        });

        var secondFeaturedProject = await repository.AddAsync(new Project
        {
            Title = "Beta",
            StartDate = new DateOnly(2026, 2, 1),
            ShortDescription = "Featured beta.",
            LongDescriptionMarkdown = "Markdown.",
            IsPublished = true,
            IsFeatured = true
        });

        var isUpdated = await repository.ReorderFeaturedProjectsAsync([
            0,
            featuredProject.Id,
            -1,
            secondFeaturedProject.Id
        ]);

        var featuredProjects = await repository.ListFeaturedAsync(5);
        var featuredOrders = featuredProjects
            .ToDictionary(project => project.Id, project => project.FeaturedOrder);

        Assert.That(isUpdated, Is.True);
        Assert.That(featuredOrders[featuredProject.Id], Is.EqualTo(0));
        Assert.That(featuredOrders[secondFeaturedProject.Id], Is.EqualTo(1));
    }

    [Test]
    public async Task ReorderFeaturedProjectsAsync_ReturnsFalse_WhenAnyFeatureProjectCannotBeFound()
    {
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

        var featureProject = await repository.AddAsync(new Project
        {
            Title = "Alpha",
            StartDate = new DateOnly(2026, 1, 1),
            ShortDescription = "Featured alpha.",
            LongDescriptionMarkdown = "Markdown.",
            IsPublished = true,
            IsFeatured = true
        });

        var isUpdated = await repository.ReorderFeaturedProjectsAsync([
            featureProject.Id,
            999
        ]);

        Assert.That(isUpdated, Is.False);
    }

    [Test]
    public async Task ReorderFeaturedProjectsAsync_SerializesWithConcurrentUnfeature()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        await using (var seedContext = new PortfolioDbContext(options))
        {
            var seedRepository = CreateRepository(seedContext);
            await seedRepository.AddAsync(CreateFeaturedProject("First", 0));
            await seedRepository.AddAsync(CreateFeaturedProject("Second", 1));
        }

        await using var reorderContext = new PortfolioDbContext(options);
        await using var unfeatureContext = new PortfolioDbContext(options);
        var reorderRepository = CreateRepository(reorderContext);
        var unfeatureRepository = CreateRepository(unfeatureContext);

        await Task.WhenAll(
            reorderRepository.ReorderFeaturedProjectsAsync([2, 1]),
            unfeatureRepository.UpdateFeaturedStateAsync(1, false));

        await using var verificationContext = new PortfolioDbContext(options);
        var firstProject = await verificationContext.Projects.SingleAsync(project => project.Id == 1);
        Assert.That(firstProject.IsFeatured, Is.False);
        Assert.That(firstProject.FeaturedOrder, Is.Null);
    }

    [Test]
    public async Task SetArchivedStateAsync_RecordsTimestamp_WhenArchivedAndClearsWhenRestored()
    {
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

        var project = await repository.AddAsync(new Project
        {
            Title = "Archivable Project",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "Live project.",
            LongDescriptionMarkdown = "Project body.",
            IsPublished = true
        });

        var archived = await repository.SetArchivedStateAsync(project.Id, true);

        Assert.That(archived, Is.Not.Null);
        Assert.That(archived!.IsArchived, Is.True);
        Assert.That(archived.ArchivedAt, Is.Not.Null);

        var restored = await repository.SetArchivedStateAsync(project.Id, false);

        Assert.That(restored, Is.Not.Null);
        Assert.That(restored!.IsArchived, Is.False);
        Assert.That(restored.ArchivedAt, Is.Null);
    }

    [Test]
    public async Task SetArchivedStateAsync_DoesNotRefreshTimestamp_WhenAlreadyArchived()
    {
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

        var project = await repository.AddAsync(new Project
        {
            Title = "Retry Project",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "Live project.",
            LongDescriptionMarkdown = "Project body.",
            IsPublished = true
        });

        var archived = await repository.SetArchivedStateAsync(project.Id, true);
        var archivedAtAtInitialArchive = archived?.ArchivedAt;

        var archivedAgain = await repository.SetArchivedStateAsync(project.Id, true);

        Assert.That(archivedAgain, Is.Not.Null);
        Assert.That(archivedAgain!.ArchivedAt, Is.EqualTo(archivedAtAtInitialArchive));
    }

    [Test]
    public async Task ListAdminAsync_UsesProjectIdAsStablePaginationTieBreaker()
    {
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);
        var sharedDate = new DateOnly(2026, 4, 1);

        for (var index = 0; index < 3; index += 1)
        {
            await repository.AddAsync(new Project
            {
                Title = "Matching Project",
                StartDate = sharedDate,
                ShortDescription = $"Matching project {index}.",
                LongDescriptionMarkdown = "Matching content.",
                IsPublished = true
            });
        }

        var firstPage = await repository.ListAdminAsync(null, [], 1, 2);
        var secondPage = await repository.ListAdminAsync(null, [], 2, 2);
        var returnedIds = firstPage.Items.Concat(secondPage.Items).Select(project => project.Id).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(returnedIds, Has.Count.EqualTo(3));
            Assert.That(returnedIds, Is.Ordered.Ascending);
            Assert.That(returnedIds.Distinct().Count(), Is.EqualTo(3));
        });
    }

    [Test]
    public async Task SetArchivedStateAsync_ReturnsLoadedRelationships_WhenStateIsUnchanged()
    {
        await using var dbContext = CreateDbContext();
        var repository = CreateRepository(dbContext);

        var project = await repository.AddAsync(new Project
        {
            Title = "Retry Project With Relationships",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "Live project.",
            LongDescriptionMarkdown = "Project body.",
            IsPublished = true,
            Screenshots =
            [
                new ProjectScreenshot
                {
                    ImageUrl = "/images/retry.png",
                    SortOrder = 0
                }
            ]
        });

        await repository.SetArchivedStateAsync(project.Id, true);
        dbContext.ChangeTracker.Clear();

        var archivedAgain = await repository.SetArchivedStateAsync(project.Id, true);

        Assert.That(archivedAgain, Is.Not.Null);
        Assert.That(archivedAgain!.Screenshots, Has.Count.EqualTo(1));
        Assert.That(archivedAgain.Screenshots.Single().ImageUrl, Is.EqualTo("/images/retry.png"));
    }

    private static PortfolioDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new PortfolioDbContext(options);
    }

    private static ProjectRepository CreateRepository(PortfolioDbContext dbContext)
    {
        return new ProjectRepository(
            dbContext,
            new ProjectTagNormalizer(dbContext),
            new FeaturedProjectSelector());
    }

    private static Project CreateFeaturedProject(string title, int featuredOrder)
    {
        return new Project
        {
            Title = title,
            StartDate = new DateOnly(2026, 1, 1),
            ShortDescription = $"{title} project.",
            LongDescriptionMarkdown = "Markdown.",
            IsPublished = true,
            IsFeatured = true,
            FeaturedOrder = featuredOrder
        };
    }

    private static ProjectTag CreateProjectTag(TagCategory category, string name)
    {
        return new ProjectTag
        {
            Tag = new Tag
            {
                Category = category,
                DisplayName = name,
                NormalizedName = name.Trim().ToUpperInvariant()
            }
        };
    }
}
