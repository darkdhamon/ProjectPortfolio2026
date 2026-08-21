using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Contracts;
using ProjectPortfolio2026.Server.Contracts.Resume;
using ProjectPortfolio2026.Server.Controllers;
using ProjectPortfolio2026.Server.Domain.Portfolio;
using ProjectPortfolio2026.Server.Infrastructure.RequestTracking;
using ProjectPortfolio2026.Server.Repositories;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class ResumeConfigurationControllerTests
{
    [Test]
    public async Task GetAsync_ReturnsConfiguredPublicResumeSource()
    {
        var controller = CreateController(new StubResumeConfigurationRepository
        {
            Configuration = new ResumeConfiguration
            {
                Id = 3,
                SourceType = ResumeSourceTypes.HostedFile,
                SourceUrl = "https://cdn.example.dev/resume.pdf",
                DisplayLabel = "Download Resume",
                Summary = "ATS-friendly PDF."
            }
        });
        controller.ControllerContext.HttpContext.Items[RequestIdContext.ItemKey] = "resume-config-request";

        var actionResult = await controller.GetAsync(CancellationToken.None);
        var okResult = actionResult.Result as OkObjectResult;
        var response = okResult?.Value as ResumeConfigurationResponse;

        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.RequestId, Is.EqualTo("resume-config-request"));
            Assert.That(response.SourceType, Is.EqualTo(ResumeSourceTypes.HostedFile));
            Assert.That(response.DisplayLabel, Is.EqualTo("Download Resume"));
            Assert.That(response.IsConfigured, Is.True);
        });
    }

    [Test]
    public async Task GetAsync_ReturnsNotFoundWhenConfigurationIsIncomplete()
    {
        const string expectedRequestId = "resume-config-request";
        var controller = CreateController(new StubResumeConfigurationRepository
        {
            Configuration = new ResumeConfiguration
            {
                SourceType = ResumeSourceTypes.HostedFile,
                SourceUrl = "https://cdn.example.dev/resume.pdf",
                DisplayLabel = null
            }
        });
        controller.ControllerContext.HttpContext.Items[RequestIdContext.ItemKey] = expectedRequestId;

        var actionResult = await controller.GetAsync(CancellationToken.None);
        var notFoundResult = actionResult.Result as NotFoundObjectResult;

        Assert.That(notFoundResult, Is.Not.Null);
        var error = notFoundResult?.Value as ApiErrorResponse;

        Assert.That(error, Is.Not.Null);
        Assert.That(error?.RequestId, Is.EqualTo(expectedRequestId));
        Assert.That(error?.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        Assert.That(error?.ErrorCode, Is.EqualTo("resume_configuration_missing"));
        Assert.That(error?.Message, Is.EqualTo("The requested resume configuration could not be found."));
    }

    private static ResumeConfigurationController CreateController(IResumeConfigurationRepository repository)
    {
        return new ResumeConfigurationController(repository)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private sealed class StubResumeConfigurationRepository : IResumeConfigurationRepository
    {
        public ResumeConfiguration? Configuration { get; set; }

        public Task<ResumeConfiguration?> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Configuration);
        }

        public Task<ResumeConfiguration> SaveAsync(ResumeConfiguration configuration, CancellationToken cancellationToken = default)
        {
            Configuration = configuration;
            return Task.FromResult(configuration);
        }
    }
}
