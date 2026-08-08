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

        public Task<List<PortfolioSocialLink>> SaveSocialLinksAsync(IEnumerable<PortfolioSocialLink> socialLinks, CancellationToken cancellationToken = default)
        {
            SavedLinks = [..socialLinks];
            return Task.FromResult(SavedLinks);
        }
    }
}
