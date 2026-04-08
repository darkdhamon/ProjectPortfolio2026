using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Contracts;
using ProjectPortfolio2026.Server.Contracts.Portfolio;
using ProjectPortfolio2026.Server.Controllers;
using ProjectPortfolio2026.Server.Domain.Portfolio;
using ProjectPortfolio2026.Server.Infrastructure.RequestTracking;
using ProjectPortfolio2026.Server.Repositories;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class PortfolioProfileControllerTests
{
    [Test]
    public async Task GetAsync_ReturnsPublicProfileWithRequestId()
    {
        var repository = new StubPortfolioProfileRepository
        {
            Profile = new PortfolioProfile
            {
                Id = 7,
                DisplayName = "Bronze Loft",
                ContactHeadline = "Reach out",
                ContactIntro = "Public intro",
                IsPublic = true,
                ContactMethods =
                [
                    new PortfolioContactMethod
                    {
                        Type = "email",
                        Label = "Email",
                        Value = "bronze@example.dev",
                        SortOrder = 1,
                        IsVisible = true
                    },
                    new PortfolioContactMethod
                    {
                        Type = "phone",
                        Label = "Phone",
                        Value = "(312) 555-0147",
                        SortOrder = 2,
                        IsVisible = false
                    }
                ],
                SocialLinks =
                [
                    new PortfolioSocialLink
                    {
                        Platform = "bad-link",
                        Label = "Bad Link",
                        Url = "javascript:alert('xss')",
                        SortOrder = 0,
                        IsVisible = true
                    },
                    new PortfolioSocialLink
                    {
                        Platform = "github",
                        Label = "GitHub",
                        Url = "https://github.com/darkdhamon",
                        SortOrder = 1,
                        IsVisible = true
                    }
                ]
            }
        };

        var controller = new PortfolioProfileController(repository)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.ControllerContext.HttpContext.Items[RequestIdContext.ItemKey] = "profile-id";

        var actionResult = await controller.GetAsync(CancellationToken.None);
        var okResult = actionResult.Result as OkObjectResult;
        var response = okResult?.Value as PortfolioProfileResponse;

        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.RequestId, Is.EqualTo("profile-id"));
            Assert.That(response.DisplayName, Is.EqualTo("Bronze Loft"));
            Assert.That(response.ContactMethods.Select(contactMethod => contactMethod.Label), Is.EqualTo(new[] { "Email" }));
            Assert.That(response.ContactMethods[0].Href, Is.EqualTo("mailto:bronze@example.dev"));
            Assert.That(response.SocialLinks.Select(socialLink => socialLink.Label), Is.EqualTo(new[] { "GitHub" }));
        });
    }

    [Test]
    public async Task GetAsync_ReturnsNotFoundWhenNoPublicProfileExists()
    {
        var controller = new PortfolioProfileController(new StubPortfolioProfileRepository())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var actionResult = await controller.GetAsync(CancellationToken.None);
        var notFoundResult = actionResult.Result as NotFoundObjectResult;
        var response = notFoundResult?.Value as ApiErrorResponse;

        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(response?.Message, Is.EqualTo("The requested portfolio profile could not be found."));
    }

    private sealed class StubPortfolioProfileRepository : IPortfolioProfileRepository
    {
        public PortfolioProfile? Profile { get; set; }

        public Task<PortfolioProfile?> GetPublicAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Profile);
        }
    }
}
