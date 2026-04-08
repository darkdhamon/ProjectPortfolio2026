using NUnit.Framework;
using ProjectPortfolio2026.Server.Domain.Portfolio;
using ProjectPortfolio2026.Server.Mappers;
using ProjectPortfolio2026.Server.Services.Interfaces;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class PortfolioProfileContractMapperTests
{
    [Test]
    public void ToResponse_MapsVisibleContactMethodsAndSocialLinks_UsingFormatter()
    {
        var profile = new PortfolioProfile
        {
            Id = 9,
            DisplayName = "Bronze Loft",
            ContactHeadline = "Reach out",
            ContactIntro = "Intro",
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
                    Type = "hidden",
                    Label = "Hidden",
                    Value = "Hidden",
                    SortOrder = 2,
                    IsVisible = false
                }
            ],
            SocialLinks =
            [
                new PortfolioSocialLink
                {
                    Platform = "github",
                    Label = "GitHub",
                    Url = "https://github.com/darkdhamon",
                    SortOrder = 1,
                    IsVisible = true
                },
                new PortfolioSocialLink
                {
                    Platform = "unsupported",
                    Label = "Unsupported",
                    Url = "ignored",
                    SortOrder = 2,
                    IsVisible = true
                }
            ]
        };

        var response = profile.ToResponse(new StubPortfolioLinkFormatter(), "request-1");

        Assert.Multiple(() =>
        {
            Assert.That(response.RequestId, Is.EqualTo("request-1"));
            Assert.That(response.ContactMethods.Select(contactMethod => contactMethod.Label), Is.EqualTo(new[] { "Email" }));
            Assert.That(response.ContactMethods[0].Href, Is.EqualTo("formatted:email"));
            Assert.That(response.SocialLinks.Select(socialLink => socialLink.Label), Is.EqualTo(new[] { "GitHub" }));
            Assert.That(response.SocialLinks[0].Url, Is.EqualTo("https://github.com/darkdhamon"));
        });
    }

    private sealed class StubPortfolioLinkFormatter : IPortfolioLinkFormatter
    {
        public string? BuildContactMethodHref(PortfolioContactMethod contactMethod)
        {
            return $"formatted:{contactMethod.Type}";
        }

        public string? BuildSocialLinkUrl(PortfolioSocialLink socialLink)
        {
            return socialLink.Platform.Equals("github", StringComparison.OrdinalIgnoreCase)
                ? socialLink.Url
                : null;
        }
    }
}
