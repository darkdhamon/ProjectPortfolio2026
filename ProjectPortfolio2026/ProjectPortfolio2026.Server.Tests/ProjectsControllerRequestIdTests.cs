using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
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
        repository.Projects.Add(new Project
        {
            Id = 5,
            Title = "Portfolio Platform",
            StartDate = new DateOnly(2026, 4, 1),
            ShortDescription = "Short summary.",
            LongDescriptionMarkdown = "Long summary."
        });

        var controller = CreateController(repository);
        controller.ControllerContext.HttpContext.Items[RequestIdContext.ItemKey] = "query-id";

        var actionResult = await controller.ListAsync(CancellationToken.None);
        var okResult = actionResult.Result as OkObjectResult;
        var response = okResult?.Value as ProjectListResponse;

        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.RequestId, Is.EqualTo("query-id"));
            Assert.That(response.Items, Has.Count.EqualTo(1));
            Assert.That(response.Items[0].RequestId, Is.EqualTo("query-id"));
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

        public Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Project>>(Projects);
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
