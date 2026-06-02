using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Contracts.Admin;
using ProjectPortfolio2026.Server.Contracts.Resume;
using ProjectPortfolio2026.Server.Controllers;
using ProjectPortfolio2026.Server.Domain.Portfolio;
using ProjectPortfolio2026.Server.Infrastructure.RequestTracking;
using ProjectPortfolio2026.Server.Repositories;
using System.Security.Claims;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class AdminResumeConfigurationControllerTests
{
    [Test]
    public async Task GetAsync_ReturnsDefaultConfigurationWhenNothingIsSavedYet()
    {
        var controller = CreateController(new StubResumeConfigurationRepository());

        var actionResult = await controller.GetAsync(CancellationToken.None);
        var okResult = actionResult.Result as OkObjectResult;
        var response = okResult?.Value as ResumeConfigurationResponse;

        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.SourceType, Is.EqualTo(ResumeSourceTypes.None));
            Assert.That(response.IsConfigured, Is.False);
            Assert.That(response.SourceUrl, Is.Null);
        });
    }

    [Test]
    public async Task UpdateAsync_ReturnsValidationProblemWhenEnabledSourceIsIncomplete()
    {
        var controller = CreateController(new StubResumeConfigurationRepository());

        var actionResult = await controller.UpdateAsync(
            new ResumeConfigurationUpdateRequest
            {
                SourceType = ResumeSourceTypes.HostedFile,
                SourceUrl = "https://cdn.example.dev/resume.pdf",
                DisplayLabel = ""
            },
            CancellationToken.None);

        Assert.That(actionResult.Result, Is.InstanceOf<BadRequestObjectResult>());
        var validationProblem = (actionResult.Result as BadRequestObjectResult)?.Value as ValidationProblemDetails;
        Assert.That(validationProblem?.Errors, Contains.Key("displayLabel"));
    }

    [Test]
    public async Task UpdateAsync_ReturnsValidationProblemWhenSourceTypeIsUnsupported()
    {
        var controller = CreateController(new StubResumeConfigurationRepository());

        var actionResult = await controller.UpdateAsync(
            new ResumeConfigurationUpdateRequest
            {
                SourceType = "sharepoint-link",
                SourceUrl = "https://cdn.example.dev/resume.pdf",
                DisplayLabel = "Download Resume"
            },
            CancellationToken.None);

        Assert.That(actionResult.Result, Is.InstanceOf<BadRequestObjectResult>());
        var validationProblem = (actionResult.Result as BadRequestObjectResult)?.Value as ValidationProblemDetails;
        Assert.That(validationProblem?.Errors, Contains.Key("sourceType"));
    }

    [Test]
    public async Task UpdateAsync_PersistsTrimmedConfiguration()
    {
        var repository = new StubResumeConfigurationRepository();
        var controller = CreateController(repository);
        controller.ControllerContext.HttpContext.Items[RequestIdContext.ItemKey] = "admin-config-request";

        var actionResult = await controller.UpdateAsync(
            new ResumeConfigurationUpdateRequest
            {
                SourceType = " Hosted-File ",
                SourceUrl = " https://cdn.example.dev/resume.pdf ",
                DisplayLabel = " Download Resume ",
                Summary = " ATS-friendly export. "
            },
            CancellationToken.None);

        var okResult = actionResult.Result as OkObjectResult;
        var response = okResult?.Value as ResumeConfigurationResponse;

        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.RequestId, Is.EqualTo("admin-config-request"));
            Assert.That(response.SourceType, Is.EqualTo(ResumeSourceTypes.HostedFile));
            Assert.That(response.SourceUrl, Is.EqualTo("https://cdn.example.dev/resume.pdf"));
            Assert.That(response.DisplayLabel, Is.EqualTo("Download Resume"));
            Assert.That(response.Summary, Is.EqualTo("ATS-friendly export."));
            Assert.That(response.IsConfigured, Is.True);
            Assert.That(repository.Configuration?.DisplayLabel, Is.EqualTo("Download Resume"));
        });
    }

    private static AdminResumeConfigurationController CreateController(IResumeConfigurationRepository repository)
    {
        return new AdminResumeConfigurationController(repository)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], "test"))
                }
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
            configuration.Id = configuration.Id == 0 ? 1 : configuration.Id;
            return Task.FromResult(configuration);
        }
    }
}
