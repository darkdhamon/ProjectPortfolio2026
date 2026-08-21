using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Contracts;
using ProjectPortfolio2026.Server.Contracts.Projects;
using ProjectPortfolio2026.Server.Controllers;
using ProjectPortfolio2026.Server.Domain.Projects;
using ProjectPortfolio2026.Server.Infrastructure.RequestTracking;
using ProjectPortfolio2026.Server.Repositories;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class AdminProjectsControllerTests
{
    [Test]
    public async Task ListAsync_ReturnsPublishedAndUnpublishedProjects()
    {
        var repository = new StubProjectRepository
        {
            Projects = [
                new Project { Id = 10, Title = "Published", IsPublished = true },
                new Project { Id = 11, Title = "Draft", IsPublished = false }
            ]
        };

        var controller = CreateController(repository);
        controller.ControllerContext.HttpContext.Items[RequestIdContext.ItemKey] = "admin-projects-request";

        var actionResult = await controller.ListAsync(default);
        var okResult = actionResult.Result as OkObjectResult;
        var response = okResult?.Value as IReadOnlyList<ProjectSummaryResponse>;

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Select(project => project.Id), Is.EquivalentTo(new[] { 10, 11 }));
        Assert.That(response!.All(project => project.RequestId == "admin-projects-request"), Is.True);
    }

    [Test]
    public async Task SetFeaturedStateAsync_ReturnsUpdatedProject_WhenProjectExists()
    {
        var repository = new StubProjectRepository
        {
            Projects = [new Project
            {
                Id = 10,
                Title = "Portfolio Refresh",
                StartDate = new DateOnly(2026, 4, 1),
                ShortDescription = "Portfolio project.",
                LongDescriptionMarkdown = "Portfolio details.",
                IsPublished = true,
                IsFeatured = false,
                FeaturedOrder = null
            }]
        };

        var controller = new AdminProjectsController(repository);

        var actionResult = await controller.SetFeaturedStateAsync(10, new ProjectFeaturedStateRequest { IsFeatured = true }, default);
        var okResult = actionResult.Result as OkObjectResult;
        var response = okResult?.Value as ProjectResponse;

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.IsFeatured, Is.True);
        Assert.That(response.Id, Is.EqualTo(10));
    }

    [Test]
    public async Task SetFeaturedStateAsync_ReturnsNotFound_WhenProjectMissing()
    {
        var repository = new StubProjectRepository();
        var controller = new AdminProjectsController(repository);

        var actionResult = await controller.SetFeaturedStateAsync(999, new ProjectFeaturedStateRequest { IsFeatured = true }, default);
        var notFoundResult = actionResult.Result as NotFoundObjectResult;
        var payload = notFoundResult?.Value as ApiErrorResponse;

        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.Message, Is.EqualTo("The requested project could not be found."));
    }

    [Test]
    public async Task SetFeaturedOrderAsync_ReturnsNoContent_WhenReorderSucceeds()
    {
        var repository = new StubProjectRepository
        {
            Projects = [
                new Project { Id = 10, Title = "Alpha", IsFeatured = true },
                new Project { Id = 11, Title = "Beta", IsFeatured = true }
            ]
        };

        var controller = new AdminProjectsController(repository);

        var actionResult = await controller.SetFeaturedOrderAsync(new ProjectFeaturedOrderUpdateRequest
        {
            ProjectIds = [11, 10]
        }, default);

        Assert.That(actionResult, Is.InstanceOf<NoContentResult>());
        Assert.That(repository.LastOrder, Is.EqualTo(new[] { 11, 10 }));
    }

    [Test]
    public async Task SetFeaturedOrderAsync_ReturnsNotFound_WhenReorderProjectCannotBeFound()
    {
        var repository = new StubProjectRepository
        {
            ReorderResult = false
        };

        var controller = new AdminProjectsController(repository);

        var actionResult = await controller.SetFeaturedOrderAsync(new ProjectFeaturedOrderUpdateRequest
        {
            ProjectIds = [10, 11]
        }, default);
        var notFoundResult = actionResult as ObjectResult;
        var payload = notFoundResult?.Value as ApiErrorResponse;

        Assert.That(notFoundResult?.StatusCode, Is.EqualTo(404));
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.Message, Is.EqualTo("The requested project order could not be applied because one or more projects were not found."));
    }

    [Test]
    public async Task SetFeaturedOrderAsync_ReturnsNoContent_WhenDuplicateProjectIdsAreSubmitted()
    {
        var repository = new StubProjectRepository
        {
            Projects = [
                new Project { Id = 10, Title = "Alpha", IsFeatured = true },
                new Project { Id = 11, Title = "Beta", IsFeatured = true }
            ]
        };

        var controller = new AdminProjectsController(repository);

        var actionResult = await controller.SetFeaturedOrderAsync(new ProjectFeaturedOrderUpdateRequest
        {
            ProjectIds = [10, 10, 11]
        }, default);

        Assert.That(actionResult, Is.InstanceOf<NoContentResult>());
        Assert.That(repository.LastOrder, Is.EqualTo(new[] { 10, 11 }));
    }

    private sealed class StubProjectRepository : IProjectRepository
    {
        public List<Project> Projects { get; init; } = [];
        public IEnumerable<int>? LastOrder { get; private set; }
        public bool ReorderResult { get; init; } = true;

        public Task<Project> AddAsync(Project project, CancellationToken cancellationToken = default)
        {
            project.Id = Projects.Count == 0 ? 1 : Projects[^1].Id + 1;
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
            return Task.FromResult(new ProjectListPage());
        }

        public Task<IReadOnlyList<ProjectListItem>> ListAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult((IReadOnlyList<ProjectListItem>)Projects
                .Select(project => new ProjectListItem
                {
                    Id = project.Id,
                    Title = project.Title,
                    IsFeatured = project.IsFeatured,
                    FeaturedOrder = project.FeaturedOrder
                })
                .ToList());
        }

        public Task<IReadOnlyList<ProjectListItem>> ListFeaturedAsync(
            int limit,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult((IReadOnlyList<ProjectListItem>)Projects
                .Where(project => project.IsFeatured)
                .Select(project => new ProjectListItem { Id = project.Id, Title = project.Title, IsFeatured = project.IsFeatured })
                .ToList());
        }

        public Task<Project?> UpdateAsync(Project project, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Project?>(project);
        }

        public Task<Project?> UpdateFeaturedStateAsync(int projectId, bool isFeatured, CancellationToken cancellationToken = default)
        {
            var project = Projects.SingleOrDefault(existing => existing.Id == projectId);
            if (project is null)
            {
                return Task.FromResult<Project?>(null);
            }

            project.IsFeatured = isFeatured;
            project.FeaturedOrder = isFeatured ? 0 : null;
            return Task.FromResult<Project?>(project);
        }

        public Task<bool> ReorderFeaturedProjectsAsync(IReadOnlyCollection<int> orderedFeaturedProjectIds, CancellationToken cancellationToken = default)
        {
            LastOrder = orderedFeaturedProjectIds.Distinct().ToList();
            if (!ReorderResult)
            {
                return Task.FromResult(false);
            }

            for (var index = 0; index < LastOrder.Count(); index += 1)
            {
                var project = Projects.Single(existing => existing.Id == LastOrder!.ElementAt(index));
                project.IsFeatured = true;
                project.FeaturedOrder = index;
            }

            return Task.FromResult(true);
        }
    }

    private static AdminProjectsController CreateController(IProjectRepository repository)
    {
        return new AdminProjectsController(repository)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }
}
