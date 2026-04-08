using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

using ProjectPortfolio2026.Server.Contracts.WorkHistory;
using ProjectPortfolio2026.Server.Controllers;
using ProjectPortfolio2026.Server.Domain.Tags;
using ProjectPortfolio2026.Server.Domain.WorkHistory;
using ProjectPortfolio2026.Server.Infrastructure.RequestTracking;
using ProjectPortfolio2026.Server.Repositories;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class WorkHistoryControllerTests
{
    [Test]
    public async Task ListAsync_ReturnsPublishedEmployerResponses_WithRequestId()
    {
        var repository = new StubEmployerRepository
        {
            Employers =
            [
                new Employer
                {
                    Id = 7,
                    Name = "Northwind Health",
                    City = "Chicago",
                    Region = "IL",
                    JobRoles =
                    [
                        new JobRole
                        {
                            Role = "Developer",
                            StartDate = new DateOnly(2022, 1, 1),
                            EndDate = new DateOnly(2023, 12, 31),
                            DescriptionMarkdown = "Previous role"
                        },
                        new JobRole
                        {
                            Role = "Senior Developer",
                            StartDate = new DateOnly(2024, 1, 1),
                            DescriptionMarkdown = "Current role",
                            JobRoleTags =
                            [
                                CreateJobRoleTag(TagCategory.Skill, "API Design"),
                                CreateJobRoleTag(TagCategory.Technology, ".NET")
                            ]
                        }
                    ]
                }
            ]
        };

        var controller = new WorkHistoryController(repository)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.ControllerContext.HttpContext.Items[RequestIdContext.ItemKey] = "work-history-id";

        var actionResult = await controller.ListAsync(CancellationToken.None);
        var okResult = actionResult.Result as OkObjectResult;
        var response = okResult?.Value as WorkHistoryResponse;

        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.RequestId, Is.EqualTo("work-history-id"));
            Assert.That(response.Items, Has.Count.EqualTo(1));
            Assert.That(response.Items[0].Name, Is.EqualTo("Northwind Health"));
            Assert.That(response.Items[0].City, Is.EqualTo("Chicago"));
            Assert.That(response.Items[0].Region, Is.EqualTo("IL"));
            Assert.That(response.Items[0].JobRoles, Has.Count.EqualTo(2));
            Assert.That(response.Items[0].JobRoles.Select(jobRole => jobRole.Role), Is.EqualTo(new[] { "Senior Developer", "Developer" }));
            Assert.That(response.Items[0].JobRoles[0].Skills, Is.EqualTo(new[] { "API Design" }));
            Assert.That(response.Items[0].JobRoles[0].Technologies, Is.EqualTo(new[] { ".NET" }));
        });
    }

    private sealed class StubEmployerRepository : IEmployerRepository
    {
        public IReadOnlyList<Employer> Employers { get; set; } = [];

        public Task<IReadOnlyList<Employer>> ListPublishedAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Employers);
        }
    }

    private static JobRoleTag CreateJobRoleTag(TagCategory category, string displayName)
    {
        return new JobRoleTag
        {
            Tag = new Tag
            {
                Category = category,
                DisplayName = displayName,
                NormalizedName = displayName.Trim().ToUpperInvariant()
            }
        };
    }
}
