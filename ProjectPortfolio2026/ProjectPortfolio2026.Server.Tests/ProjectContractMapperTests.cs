using NUnit.Framework;
using ProjectPortfolio2026.Server.Contracts.Projects;
using ProjectPortfolio2026.Server.Domain.Projects;
using ProjectPortfolio2026.Server.Domain.Tags;
using ProjectPortfolio2026.Server.Mappers;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class ProjectContractMapperTests
{
    [Test]
    public void ToDomain_MapsNestedRequestGraph_AndFiltersBlankTagValues()
    {
        var request = new ProjectRequest
        {
            RequestId = "request-123",
            Title = " Portfolio Platform ",
            StartDate = new DateOnly(2026, 4, 1),
            EndDate = new DateOnly(2026, 4, 2),
            PrimaryImageUrl = "https://example.test/hero.png",
            ShortDescription = "Short summary.",
            LongDescriptionMarkdown = "Long summary.",
            GitHubUrl = "https://github.com/example/repo",
            DemoUrl = "https://example.test/demo",
            IsPublished = true,
            IsFeatured = true,
            Screenshots =
            [
                new ProjectScreenshotRequest
                {
                    ImageUrl = "https://example.test/shot-1.png",
                    Caption = "Shot 1",
                    SortOrder = 2
                }
            ],
            DeveloperRoles = [" Backend ", " ", "Architect"],
            Technologies = [" .NET ", "", "SQL Server"],
            Skills = [" Testing ", " ", "API Design"],
            Collaborators =
            [
                new ProjectCollaboratorRequest
                {
                    Name = "Taylor",
                    GitHubProfileUrl = "https://github.com/taylor",
                    WebsiteUrl = "https://taylor.dev",
                    PhotoUrl = "https://example.test/taylor.png",
                    Roles = [" Designer ", "", "UX"]
                }
            ],
            Milestones =
            [
                new ProjectMilestoneRequest
                {
                    Title = "Launch",
                    TargetDate = new DateOnly(2026, 5, 1),
                    CompletedOn = new DateOnly(2026, 5, 2),
                    Description = "Public release."
                }
            ]
        };

        var project = request.ToDomain();

        Assert.Multiple(() =>
        {
            Assert.That(project.Title, Is.EqualTo(" Portfolio Platform "));
            Assert.That(project.StartDate, Is.EqualTo(new DateOnly(2026, 4, 1)));
            Assert.That(project.EndDate, Is.EqualTo(new DateOnly(2026, 4, 2)));
            Assert.That(project.PrimaryImageUrl, Is.EqualTo("https://example.test/hero.png"));
            Assert.That(project.GitHubUrl, Is.EqualTo("https://github.com/example/repo"));
            Assert.That(project.DemoUrl, Is.EqualTo("https://example.test/demo"));
            Assert.That(project.IsPublished, Is.True);
            Assert.That(project.IsFeatured, Is.True);
            Assert.That(project.Screenshots, Has.Count.EqualTo(1));
            Assert.That(project.Screenshots[0].Caption, Is.EqualTo("Shot 1"));
            Assert.That(project.DeveloperRoles.Select(role => role.Name), Is.EquivalentTo(["Backend", "Architect"]));
            Assert.That(project.ProjectTags.Where(projectTag => projectTag.Tag?.Category == TagCategory.Technology).Select(projectTag => projectTag.Tag!.DisplayName), Is.EquivalentTo([".NET", "SQL Server"]));
            Assert.That(project.ProjectTags.Where(projectTag => projectTag.Tag?.Category == TagCategory.Skill).Select(projectTag => projectTag.Tag!.DisplayName), Is.EquivalentTo(["Testing", "API Design"]));
            Assert.That(project.Collaborators, Has.Count.EqualTo(1));
            Assert.That(project.Collaborators[0].Name, Is.EqualTo("Taylor"));
            Assert.That(project.Collaborators[0].GitHubProfileUrl, Is.EqualTo("https://github.com/taylor"));
            Assert.That(project.Collaborators[0].WebsiteUrl, Is.EqualTo("https://taylor.dev"));
            Assert.That(project.Collaborators[0].PhotoUrl, Is.EqualTo("https://example.test/taylor.png"));
            Assert.That(project.Collaborators[0].Roles.Select(role => role.Name), Is.EquivalentTo(["Designer", "UX"]));
            Assert.That(project.Milestones, Has.Count.EqualTo(1));
            Assert.That(project.Milestones[0].CompletedOn, Is.EqualTo(new DateOnly(2026, 5, 2)));
            Assert.That(project.Milestones[0].Description, Is.EqualTo("Public release."));
        });
    }

    [Test]
    public void ApplyTo_ReplacesExistingProjectValues_AndCollections()
    {
        var existingProject = new Project
        {
            Title = "Old Title",
            StartDate = new DateOnly(2025, 1, 1),
            ShortDescription = "Old short.",
            LongDescriptionMarkdown = "Old long.",
            DeveloperRoles = [new ProjectDeveloperRole { Name = "Old Role" }],
            ProjectTags =
            [
                CreateProjectTag(TagCategory.Technology, "Old Tech"),
                CreateProjectTag(TagCategory.Skill, "Old Skill")
            ],
            Screenshots = [new ProjectScreenshot { ImageUrl = "https://example.test/old.png", SortOrder = 9 }],
            Collaborators =
            [
                new ProjectCollaborator
                {
                    Name = "Old Collaborator",
                    Roles = [new ProjectCollaboratorRole { Name = "Old Collaborator Role" }]
                }
            ],
            Milestones = [new ProjectMilestone { Title = "Old Milestone" }]
        };

        var request = new ProjectRequest
        {
            Title = "New Title",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 2, 1),
            PrimaryImageUrl = "https://example.test/new.png",
            ShortDescription = "New short.",
            LongDescriptionMarkdown = "New long.",
            GitHubUrl = "https://github.com/example/new",
            DemoUrl = "https://example.test/new-demo",
            IsPublished = true,
            IsFeatured = true,
            DeveloperRoles = ["Backend"],
            Technologies = ["SQL Server"],
            Skills = ["Testing"],
            Screenshots = [new ProjectScreenshotRequest { ImageUrl = "https://example.test/new-shot.png", Caption = "New", SortOrder = 1 }],
            Collaborators = [new ProjectCollaboratorRequest { Name = "New Collaborator", Roles = ["Reviewer"] }],
            Milestones = [new ProjectMilestoneRequest { Title = "New Milestone", Description = "Updated milestone." }]
        };

        request.ApplyTo(existingProject);

        Assert.Multiple(() =>
        {
            Assert.That(existingProject.Title, Is.EqualTo("New Title"));
            Assert.That(existingProject.EndDate, Is.EqualTo(new DateOnly(2026, 2, 1)));
            Assert.That(existingProject.PrimaryImageUrl, Is.EqualTo("https://example.test/new.png"));
            Assert.That(existingProject.GitHubUrl, Is.EqualTo("https://github.com/example/new"));
            Assert.That(existingProject.DemoUrl, Is.EqualTo("https://example.test/new-demo"));
            Assert.That(existingProject.IsPublished, Is.True);
            Assert.That(existingProject.IsFeatured, Is.True);
            Assert.That(existingProject.DeveloperRoles.Select(role => role.Name), Is.EquivalentTo(["Backend"]));
            Assert.That(existingProject.ProjectTags.Where(projectTag => projectTag.Tag?.Category == TagCategory.Technology).Select(projectTag => projectTag.Tag!.DisplayName), Is.EquivalentTo(["SQL Server"]));
            Assert.That(existingProject.ProjectTags.Where(projectTag => projectTag.Tag?.Category == TagCategory.Skill).Select(projectTag => projectTag.Tag!.DisplayName), Is.EquivalentTo(["Testing"]));
            Assert.That(existingProject.Screenshots.Select(screenshot => screenshot.Caption), Is.EquivalentTo(["New"]));
            Assert.That(existingProject.Collaborators.Select(collaborator => collaborator.Name), Is.EquivalentTo(["New Collaborator"]));
            Assert.That(existingProject.Collaborators[0].Roles.Select(role => role.Name), Is.EquivalentTo(["Reviewer"]));
            Assert.That(existingProject.Milestones.Select(milestone => milestone.Title), Is.EquivalentTo(["New Milestone"]));
        });
    }

    [Test]
    public void ToDomain_TreatsNullCollectionsAsEmpty()
    {
        var request = new ProjectRequest
        {
            Title = "Null-safe project",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "Short summary.",
            LongDescriptionMarkdown = "Long summary.",
            Screenshots = null!,
            DeveloperRoles = null!,
            Technologies = null!,
            Skills = null!,
            Collaborators = null!,
            Milestones = null!
        };

        var project = request.ToDomain();

        Assert.Multiple(() =>
        {
            Assert.That(project.Screenshots, Is.Empty);
            Assert.That(project.DeveloperRoles, Is.Empty);
            Assert.That(project.ProjectTags, Is.Empty);
            Assert.That(project.Collaborators, Is.Empty);
            Assert.That(project.Milestones, Is.Empty);
        });
    }

    [Test]
    public void ToDomain_TreatsNullCollaboratorRolesAsEmpty()
    {
        var request = new ProjectRequest
        {
            Title = "Null collaborator roles",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "Short summary.",
            LongDescriptionMarkdown = "Long summary.",
            Collaborators =
            [
                new ProjectCollaboratorRequest
                {
                    Name = "Taylor",
                    Roles = null!
                }
            ]
        };

        var project = request.ToDomain();

        Assert.That(project.Collaborators, Has.Count.EqualTo(1));
        Assert.That(project.Collaborators[0].Roles, Is.Empty);
    }

    [Test]
    public void ToResponse_MapsAndSortsNestedCollections_WithRequestId()
    {
        var project = new Project
        {
            Id = 42,
            Title = "Portfolio Platform",
            StartDate = new DateOnly(2026, 4, 1),
            EndDate = new DateOnly(2026, 4, 10),
            PrimaryImageUrl = "https://example.test/hero.png",
            ShortDescription = "Short summary.",
            LongDescriptionMarkdown = "Long summary.",
            GitHubUrl = "https://github.com/example/repo",
            DemoUrl = "https://example.test/demo",
            IsPublished = true,
            IsFeatured = true,
            Screenshots =
            [
                new ProjectScreenshot { ImageUrl = "https://example.test/shot-2.png", Caption = "Second", SortOrder = 2 },
                new ProjectScreenshot { ImageUrl = "https://example.test/shot-1.png", Caption = "First", SortOrder = 1 }
            ],
            DeveloperRoles =
            [
                new ProjectDeveloperRole { Name = "Backend" },
                new ProjectDeveloperRole { Name = "Architect" }
            ],
            ProjectTags =
            [
                CreateProjectTag(TagCategory.Technology, "SQL Server"),
                CreateProjectTag(TagCategory.Technology, ".NET"),
                CreateProjectTag(TagCategory.Skill, "API Design"),
                CreateProjectTag(TagCategory.Skill, "Testing")
            ],
            Collaborators =
            [
                new ProjectCollaborator
                {
                    Name = "Zed",
                    GitHubProfileUrl = "https://github.com/zed",
                    WebsiteUrl = "https://zed.dev",
                    PhotoUrl = "https://example.test/zed.png",
                    Roles =
                    [
                        new ProjectCollaboratorRole { Name = "Designer" },
                        new ProjectCollaboratorRole { Name = "Art Director" }
                    ]
                },
                new ProjectCollaborator
                {
                    Name = "Alex",
                    Roles = [new ProjectCollaboratorRole { Name = "Reviewer" }]
                }
            ],
            Milestones =
            [
                new ProjectMilestone { Title = "Beta", TargetDate = new DateOnly(2026, 6, 1), Description = "Beta" },
                new ProjectMilestone { Title = "Alpha", TargetDate = new DateOnly(2026, 5, 1), Description = "Alpha" }
            ]
        };

        var response = project.ToResponse("request-456");

        Assert.Multiple(() =>
        {
            Assert.That(response.RequestId, Is.EqualTo("request-456"));
            Assert.That(response.Id, Is.EqualTo(42));
            Assert.That(response.PrimaryImageUrl, Is.EqualTo("https://example.test/hero.png"));
            Assert.That(response.Screenshots.Select(screenshot => screenshot.Caption), Is.EqualTo(["First", "Second"]));
            Assert.That(response.DeveloperRoles, Is.EqualTo(["Architect", "Backend"]));
            Assert.That(response.Technologies, Is.EqualTo([".NET", "SQL Server"]));
            Assert.That(response.Skills, Is.EqualTo(["API Design", "Testing"]));
            Assert.That(response.Collaborators.Select(collaborator => collaborator.Name), Is.EqualTo(["Alex", "Zed"]));
            Assert.That(response.Collaborators[1].GitHubProfileUrl, Is.EqualTo("https://github.com/zed"));
            Assert.That(response.Collaborators[1].WebsiteUrl, Is.EqualTo("https://zed.dev"));
            Assert.That(response.Collaborators[1].PhotoUrl, Is.EqualTo("https://example.test/zed.png"));
            Assert.That(response.Collaborators[1].Roles, Is.EqualTo(["Art Director", "Designer"]));
            Assert.That(response.Milestones.Select(milestone => milestone.Title), Is.EqualTo(["Alpha", "Beta"]));
            Assert.That(response.Milestones.Select(milestone => milestone.Description), Is.EqualTo(["Alpha", "Beta"]));
        });
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
