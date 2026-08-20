using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Contracts.Admin;
using ProjectPortfolio2026.Server.Controllers;
using ProjectPortfolio2026.Server.Domain.Portfolio;
using ProjectPortfolio2026.Server.Repositories;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class AdminSocialLinksControllerTests
{
    [Test]
    public async Task GetAsync_ReturnsSortedLinksForAdministration()
    {
        var repository = new StubPortfolioProfileRepository
        {
            SocialLinks =
            [
                new PortfolioSocialLink
                {
                    Id = 2,
                    Platform = "github",
                    Label = "GitHub",
                    Url = "https://github.com/darkdhamon",
                    SortOrder = 2,
                    IsVisible = true
                },
                new PortfolioSocialLink
                {
                    Id = 1,
                    Platform = "linkedin",
                    Label = "LinkedIn",
                    Url = "https://www.linkedin.com/in/darkdhamon",
                    SortOrder = 1,
                    IsVisible = true
                }
            ]
        };

        var controller = CreateController(repository);
        var actionResult = await controller.GetAsync(CancellationToken.None);

        var okResult = actionResult.Result as OkObjectResult;
        var response = okResult?.Value as List<AdminSocialLinkResponse>;

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Select(link => link.Label), Is.EqualTo(new[] { "LinkedIn", "GitHub" }));
    }

    [Test]
    public async Task UpdateAsync_ReturnsValidationProblemWhenRequiredValuesAreMissing()
    {
        var repository = new StubPortfolioProfileRepository();
        var controller = CreateController(repository);

        var actionResult = await controller.UpdateAsync(
            new AdminSocialLinksUpdateRequest
            {
                SocialLinks =
                [
                    new AdminSocialLinkRequest
                    {
                        Platform = "",
                        Label = "GitHub",
                        Url = "https://github.com/darkdhamon"
                    }
                ]
            },
            CancellationToken.None);

        Assert.That(actionResult.Result, Is.InstanceOf<BadRequestObjectResult>());
        var validationProblem = (actionResult.Result as BadRequestObjectResult)?.Value as ValidationProblemDetails;
        Assert.That(validationProblem?.Errors, Contains.Key("socialLinks[0].platform"));
    }

    [TestCase("platform", 51, "socialLinks[0].platform")]
    [TestCase("label", 101, "socialLinks[0].label")]
    [TestCase("url", 501, "socialLinks[0].url")]
    [TestCase("handle", 151, "socialLinks[0].handle")]
    [TestCase("summary", 501, "socialLinks[0].summary")]
    public async Task UpdateAsync_ReturnsValidationProblemWhenFieldExceedsDatabaseLimit(
        string field,
        int length,
        string expectedErrorKey)
    {
        var repository = new StubPortfolioProfileRepository();
        var controller = CreateController(repository);

        var actionResult = await controller.UpdateAsync(CreateRequestWithFieldLength(field, length), CancellationToken.None);

        Assert.That(actionResult.Result, Is.InstanceOf<BadRequestObjectResult>());
        var validationProblem = (actionResult.Result as BadRequestObjectResult)?.Value as ValidationProblemDetails;
        Assert.That(validationProblem?.Errors, Contains.Key(expectedErrorKey));
    }

    [Test]
    public async Task UpdateAsync_PersistsNormalizedSocialLinks()
    {
        var repository = new StubPortfolioProfileRepository();
        var controller = CreateController(repository);

        var actionResult = await controller.UpdateAsync(
            new AdminSocialLinksUpdateRequest
            {
                SocialLinks =
                [
                    new AdminSocialLinkRequest
                    {
                        Id = 9,
                        Platform = " github ",
                        Label = " GitHub Profile ",
                        Url = " https://github.com/darkdhamon ",
                        IsVisible = true,
                        SortOrder = 4
                    }
                ]
            },
            CancellationToken.None);

        var okResult = actionResult.Result as OkObjectResult;
        var response = okResult?.Value as List<AdminSocialLinkResponse>;

        Assert.That(response, Is.Not.Null);
        Assert.That(response, Has.Count.EqualTo(1));
        Assert.That(repository.SavedLinks, Has.Count.EqualTo(1));
        Assert.That(repository.SavedLinks![0].Platform, Is.EqualTo("github"));
        Assert.Multiple(() =>
        {
            Assert.That(response![0].Id, Is.EqualTo(9));
            Assert.That(response![0].Platform, Is.EqualTo("github"));
            Assert.That(response![0].Label, Is.EqualTo("GitHub Profile"));
            Assert.That(response![0].Url, Is.EqualTo("https://github.com/darkdhamon"));
        });
    }

    [Test]
    public async Task UpdateAsync_ReturnsConflictWhenNoPublicProfileExists()
    {
        var repository = new StubPortfolioProfileRepository
        {
            SaveResult = null
        };
        var controller = CreateController(repository);

        var actionResult = await controller.UpdateAsync(
            new AdminSocialLinksUpdateRequest
            {
                SocialLinks =
                [
                    new AdminSocialLinkRequest
                    {
                        Platform = "github",
                        Label = "GitHub",
                        Url = "https://github.com/darkdhamon"
                    }
                ]
            },
            CancellationToken.None);

        Assert.That(actionResult.Result, Is.InstanceOf<ConflictObjectResult>());
        var problem = (actionResult.Result as ConflictObjectResult)?.Value as ProblemDetails;
        Assert.That(problem?.Detail, Does.Contain("No public portfolio profile"));
    }

    private static AdminSocialLinksUpdateRequest CreateRequestWithFieldLength(string field, int length)
    {
        var excessiveValue = new string('a', length);

        return new AdminSocialLinksUpdateRequest
        {
            SocialLinks =
            [
                new AdminSocialLinkRequest
                {
                    Platform = field == "platform" ? excessiveValue : "github",
                    Label = field == "label" ? excessiveValue : "GitHub",
                    Url = field == "url" ? BuildLongUrl(length) : "https://github.com/darkdhamon",
                    Handle = field == "handle" ? excessiveValue : "handle",
                    Summary = field == "summary" ? excessiveValue : "summary"
                }
            ]
        };
    }

    private static string BuildLongUrl(int length)
    {
        const string urlPrefix = "https://example.com/";
        if (length <= urlPrefix.Length)
        {
            return urlPrefix[..length];
        }

        return $"{urlPrefix}{new string('a', length - urlPrefix.Length)}";
    }

    private static AdminSocialLinksController CreateController(IPortfolioProfileRepository repository)
    {
        return new AdminSocialLinksController(repository)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private sealed class StubPortfolioProfileRepository : IPortfolioProfileRepository
    {
        public List<PortfolioSocialLink> SocialLinks { get; set; } = [];

        public List<PortfolioSocialLink>? SavedLinks { get; private set; }

        public List<PortfolioSocialLink>? SaveResult { get; set; } = [];

        public Task<PortfolioProfile?> GetPublicAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PortfolioProfile?>(null);
        }

        public Task<List<PortfolioSocialLink>> GetSocialLinksAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SocialLinks
                .OrderBy(link => link.SortOrder)
                .ThenBy(link => link.Label)
                .ToList());
        }

        public Task<List<PortfolioSocialLink>?> SaveSocialLinksAsync(IEnumerable<PortfolioSocialLink> socialLinks, CancellationToken cancellationToken = default)
        {
            SavedLinks = [..socialLinks];
            return Task.FromResult(SaveResult is null ? null : SavedLinks);
        }
    }
}
