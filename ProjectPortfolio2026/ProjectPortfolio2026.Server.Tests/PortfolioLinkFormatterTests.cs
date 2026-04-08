using NUnit.Framework;
using ProjectPortfolio2026.Server.Domain.Portfolio;
using ProjectPortfolio2026.Server.Services.Implementations;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class PortfolioLinkFormatterTests
{
    private readonly PortfolioLinkFormatter formatter = new();

    [Test]
    public void BuildContactMethodHref_GeneratesMailToAndTelephoneLinks()
    {
        var emailContact = new PortfolioContactMethod
        {
            Type = "email",
            Value = "bronze@example.dev"
        };
        var phoneContact = new PortfolioContactMethod
        {
            Type = "phone",
            Value = "(312) 555-0147"
        };

        Assert.Multiple(() =>
        {
            Assert.That(formatter.BuildContactMethodHref(emailContact), Is.EqualTo("mailto:bronze@example.dev"));
            Assert.That(formatter.BuildContactMethodHref(phoneContact), Is.EqualTo("tel:3125550147"));
        });
    }

    [Test]
    public void BuildContactMethodHref_FiltersUnsafeOrInvalidValues()
    {
        var customContact = new PortfolioContactMethod
        {
            Type = "portfolio",
            Value = "Portfolio site",
            Href = "javascript:alert('xss')"
        };
        var invalidEmailContact = new PortfolioContactMethod
        {
            Type = "email",
            Value = "not-an-email"
        };

        Assert.Multiple(() =>
        {
            Assert.That(formatter.BuildContactMethodHref(customContact), Is.Null);
            Assert.That(formatter.BuildContactMethodHref(invalidEmailContact), Is.Null);
        });
    }

    [Test]
    public void BuildSocialLinkUrl_OnlyAllowsHttpAndHttps()
    {
        var safeSocialLink = new PortfolioSocialLink
        {
            Url = "https://github.com/darkdhamon"
        };
        var unsafeSocialLink = new PortfolioSocialLink
        {
            Url = "javascript:alert('xss')"
        };

        Assert.Multiple(() =>
        {
            Assert.That(formatter.BuildSocialLinkUrl(safeSocialLink), Is.EqualTo("https://github.com/darkdhamon"));
            Assert.That(formatter.BuildSocialLinkUrl(unsafeSocialLink), Is.Null);
        });
    }
}
