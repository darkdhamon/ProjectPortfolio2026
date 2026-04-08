using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Data;
using ProjectPortfolio2026.Server.Domain.Portfolio;
using ProjectPortfolio2026.Server.Repositories;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class PortfolioProfileRepositoryTests
{
    [Test]
    public async Task GetPublicAsync_ReturnsOnlyPublicProfileWithLoadedCollections()
    {
        await using var dbContext = CreateDbContext();
        dbContext.PortfolioProfiles.AddRange(
            new PortfolioProfile
            {
                DisplayName = "Hidden Profile",
                ContactHeadline = "Hidden",
                ContactIntro = "Hidden intro",
                IsPublic = false
            },
            new PortfolioProfile
            {
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
                        SortOrder = 2,
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
                    }
                ]
            });
        await dbContext.SaveChangesAsync();

        var repository = new PortfolioProfileRepository(dbContext);
        var profile = await repository.GetPublicAsync();

        Assert.That(profile, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(profile!.DisplayName, Is.EqualTo("Bronze Loft"));
            Assert.That(profile.ContactMethods.Select(contactMethod => contactMethod.Label), Is.EqualTo(new[] { "Email" }));
            Assert.That(profile.SocialLinks.Select(socialLink => socialLink.Label), Is.EqualTo(new[] { "GitHub" }));
        });
    }

    private static PortfolioDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new PortfolioDbContext(options);
    }
}
