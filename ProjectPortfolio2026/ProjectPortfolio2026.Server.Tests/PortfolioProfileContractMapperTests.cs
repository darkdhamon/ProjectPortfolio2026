using NUnit.Framework;
using ProjectPortfolio2026.Server.Domain.Portfolio;
using ProjectPortfolio2026.Server.Mappers;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class PortfolioProfileContractMapperTests
{
    [Test]
    public void ToResponse_GeneratesEmailAndPhoneLinks_AndFiltersUnsafeLinks()
    {
        var profile = new PortfolioProfile
        {
            Id = 9,
            DisplayName = "Bronze Loft",
            ContactHeadline = "Reach out",
            ContactIntro = "Intro",
            IsPublic = true,
            ContactMethods =
            [
                new PortfolioContactMethod
                {
                    Type = "email",
                    Label = "Email",
                    Value = "bronze@example.dev",
                    Href = "javascript:alert('xss')",
                    SortOrder = 1,
                    IsVisible = true
                },
                new PortfolioContactMethod
                {
                    Type = "phone",
                    Label = "Phone",
                    Value = "(312) 555-0147",
                    Href = "https://ignored.example.test",
                    SortOrder = 2,
                    IsVisible = true
                },
                new PortfolioContactMethod
                {
                    Type = "portfolio",
                    Label = "Website",
                    Value = "Portfolio site",
                    Href = "javascript:alert('xss')",
                    SortOrder = 3,
                    IsVisible = true
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
                    Platform = "unsafe",
                    Label = "Unsafe",
                    Url = "javascript:alert('xss')",
                    SortOrder = 2,
                    IsVisible = true
                }
            ]
        };

        var response = profile.ToResponse("request-1");

        Assert.Multiple(() =>
        {
            Assert.That(response.ContactMethods, Has.Count.EqualTo(3));
            Assert.That(response.ContactMethods[0].Href, Is.EqualTo("mailto:bronze@example.dev"));
            Assert.That(response.ContactMethods[1].Href, Is.EqualTo("tel:3125550147"));
            Assert.That(response.ContactMethods[2].Href, Is.Null);
            Assert.That(response.SocialLinks.Select(link => link.Label), Is.EqualTo(new[] { "GitHub" }));
            Assert.That(response.SocialLinks[0].Url, Is.EqualTo("https://github.com/darkdhamon"));
        });
    }
}
