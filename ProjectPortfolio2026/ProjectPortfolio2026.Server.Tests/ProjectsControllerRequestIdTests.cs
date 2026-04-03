using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Contracts;
using ProjectPortfolio2026.Server.Contracts.Projects;
using ProjectPortfolio2026.Server.Controllers;
using ProjectPortfolio2026.Server.Domain.Projects;
using ProjectPortfolio2026.Server.Infrastructure.RequestTracking;
using ProjectPortfolio2026.Server.Repositories;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class ProjectsControllerRequestIdTests
{
    [Test]
    public async Task ListAsync_UsesQueryStringRequestId_OverHeader()
    {
        var repository = new StubProjectRepository();
        repository.ListResult = new ProjectListPage
        {
            Items =
            [
                new ProjectListItem
                {
                    Id = 5,
                    Title = "Portfolio Platform",
                    StartDate = new DateOnly(2026, 4, 1),
                    ShortDescription = "Short summary."
                }
            ],
            Page = 2,
            PageSize = 3,
            TotalCount = 7,
            HasMore = true,
            AvailableSkills = ["API Design", "React"]
        };

        var controller = CreateController(repository);
        controller.ControllerContext.HttpContext.Items[RequestIdContext.ItemKey] = "query-id";

        var actionResult = await controller.ListAsync(
            new ProjectListQueryRequest
            {
                Search = "portfolio",
                Skills = "React, API Design",
                Page = 2,
                PageSize = 3
            },
            CancellationToken.None);
        var okResult = actionResult.Result as OkObjectResult;
        var response = okResult?.Value as ProjectListResponse;

        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.RequestId, Is.EqualTo("query-id"));
            Assert.That(response.Items, Has.Count.EqualTo(1));
            Assert.That(response.Items[0].RequestId, Is.EqualTo("query-id"));
            Assert.That(response.Page, Is.EqualTo(2));
            Assert.That(response.PageSize, Is.EqualTo(3));
            Assert.That(response.TotalCount, Is.EqualTo(7));
            Assert.That(response.HasMore, Is.True);
            Assert.That(response.AvailableSkills, Is.EqualTo(new[] { "API Design", "React" }));
            Assert.That(repository.LastListSearch, Is.EqualTo("portfolio"));
            Assert.That(repository.LastListSkills, Is.EquivalentTo(new[] { "React", "API Design" }));
            Assert.That(repository.LastListPage, Is.EqualTo(2));
            Assert.That(repository.LastListPageSize, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task ListFeaturedAsync_UsesRequestIdForFeaturedProjectSummaries()
    {
        var repository = new StubProjectRepository();
        repository.FeaturedResult =
        [
            new ProjectListItem
            {
                Id = 8,
                Title = "Featured Portfolio",
                StartDate = new DateOnly(2026, 4, 1),
                ShortDescription = "Featured summary.",
                IsFeatured = true
            }
        ];

        var controller = CreateController(repository);
        controller.ControllerContext.HttpContext.Items[RequestIdContext.ItemKey] = "featured-id";

        var actionResult = await controller.ListFeaturedAsync(5, CancellationToken.None);
        var okResult = actionResult.Result as OkObjectResult;
        var response = okResult?.Value as FeaturedProjectsResponse;

        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.RequestId, Is.EqualTo("featured-id"));
            Assert.That(response.Items, Has.Count.EqualTo(1));
            Assert.That(response.Items[0].RequestId, Is.EqualTo("featured-id"));
        });
    }

    [Test]
    public async Task CreateAsync_UsesBodyRequestId_OverQueryStringAndHeader()
    {
        var repository = new StubProjectRepository();
        var controller = CreateController(repository);
        controller.ControllerContext.HttpContext.Items[RequestIdContext.ItemKey] = "body-id";

        var request = new ProjectRequest
        {
            Title = "Portfolio Platform",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "Short summary.",
            LongDescriptionMarkdown = "Long summary."
        };

        var actionResult = await controller.CreateAsync(request, CancellationToken.None);
        var createdResult = actionResult.Result as CreatedAtActionResult;
        var response = createdResult?.Value as ProjectResponse;

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.RequestId, Is.EqualTo("body-id"));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsPublishedProjectWithRequestId()
    {
        var repository = new StubProjectRepository();
        repository.Projects.Add(new Project
        {
            Id = 24,
            Title = "Portfolio Platform",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "Short summary.",
            LongDescriptionMarkdown = "Long summary.",
            IsPublished = true
        });

        var controller = CreateController(repository);
        controller.ControllerContext.HttpContext.Items[RequestIdContext.ItemKey] = "detail-id";

        var actionResult = await controller.GetByIdAsync(24, CancellationToken.None);
        var okResult = actionResult.Result as OkObjectResult;
        var response = okResult?.Value as ProjectResponse;

        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.Id, Is.EqualTo(24));
            Assert.That(response.RequestId, Is.EqualTo("detail-id"));
        });
    }

    [Test]
    public async Task GetByIdAsync_ReturnsNotFoundForUnpublishedProject()
    {
        var repository = new StubProjectRepository();
        repository.Projects.Add(new Project
        {
            Id = 29,
            Title = "Hidden Draft",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "Internal only.",
            LongDescriptionMarkdown = "Draft.",
            IsPublished = false
        });

        var controller = CreateController(repository);

        var actionResult = await controller.GetByIdAsync(29, CancellationToken.None);
        var notFoundResult = actionResult.Result as NotFoundObjectResult;
        var response = notFoundResult?.Value as ApiErrorResponse;

        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(response?.Message, Is.EqualTo("The requested project could not be found."));
    }

    private static ProjectsController CreateController(IProjectRepository repository)
    {
        return new ProjectsController(repository)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private sealed class StubProjectRepository : IProjectRepository
    {
        public List<Project> Projects { get; } = [];

        public ProjectListPage ListResult { get; set; } = new();

        public IReadOnlyList<ProjectListItem> FeaturedResult { get; set; } = [];

        public string? LastListSearch { get; private set; }

        public IReadOnlyCollection<string> LastListSkills { get; private set; } = [];

        public int LastListPage { get; private set; }

        public int LastListPageSize { get; private set; }

        public Task<Project> AddAsync(Project project, CancellationToken cancellationToken = default)
        {
            project.Id = Projects.Count == 0 ? 1 : Projects.Max(existing => existing.Id) + 1;
            Projects.Add(project);
            return Task.FromResult(project);
        }

        public Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Projects.SingleOrDefault(project => project.Id == id));
        }

        public Task<ProjectListPage> ListAsync(
            string? search,
            IReadOnlyCollection<string> skillFilters,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            LastListSearch = search;
            LastListSkills = skillFilters;
            LastListPage = page;
            LastListPageSize = pageSize;
            return Task.FromResult(ListResult);
        }

        public Task<IReadOnlyList<ProjectListItem>> ListFeaturedAsync(
            int limit,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(FeaturedResult);
        }

        public Task<Project?> UpdateAsync(Project project, CancellationToken cancellationToken = default)
        {
            var existingIndex = Projects.FindIndex(existing => existing.Id == project.Id);
            if (existingIndex < 0)
            {
                return Task.FromResult<Project?>(null);
            }

            Projects[existingIndex] = project;
            return Task.FromResult<Project?>(project);
        }
    }
}
