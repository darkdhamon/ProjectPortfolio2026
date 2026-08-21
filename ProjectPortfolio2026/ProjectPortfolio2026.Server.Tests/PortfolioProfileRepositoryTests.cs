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
    public async Task GetPublicAsync_ReturnsNewestPublicProfileWithLoadedCollections()
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
                DisplayName = "Older Public Profile",
                ContactHeadline = "Older",
                ContactIntro = "Older intro",
                IsPublic = true
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

    [Test]
    public async Task GetSocialLinksAsync_ReturnsOnlyPublicProfileSocialLinks()
    {
        await using var dbContext = CreateDbContext();
        dbContext.PortfolioProfiles.Add(new PortfolioProfile
        {
            DisplayName = "Visible Profile",
            ContactHeadline = "Open for contact",
            ContactIntro = "Visible profile",
            IsPublic = true,
            SocialLinks =
            [
                new PortfolioSocialLink
                {
                    Platform = "github",
                    Label = "GitHub",
                    Url = "https://github.com/darkdhamon",
                    SortOrder = 2,
                    IsVisible = true
                },
                new PortfolioSocialLink
                {
                    Platform = "linkedin",
                    Label = "LinkedIn",
                    Url = "https://www.linkedin.com/in/darkdhamon",
                    SortOrder = 1,
                    IsVisible = true
                }
            ]
        });
        dbContext.PortfolioProfiles.Add(new PortfolioProfile
        {
            DisplayName = "Hidden Profile",
            ContactHeadline = "Draft profile",
            ContactIntro = "Hidden profile",
            IsPublic = false,
            SocialLinks =
            [
                new PortfolioSocialLink
                {
                    Platform = "x",
                    Label = "Private",
                    Url = "https://x.com/private",
                    SortOrder = 1,
                    IsVisible = true
                }
            ]
        });
        await dbContext.SaveChangesAsync();

        var repository = new PortfolioProfileRepository(dbContext);
        var links = await repository.GetSocialLinksAsync();

        Assert.That(links.Select(link => link.Label), Is.EqualTo(new[] { "GitHub", "LinkedIn" }));
    }

    [Test]
    public async Task SaveSocialLinksAsync_UpdatesExistingAndAddsNewLinks()
    {
        await using var dbContext = CreateDbContext();
        var profile = new PortfolioProfile
        {
            DisplayName = "Public Profile",
            ContactHeadline = "Contact updates",
            ContactIntro = "Contact section",
            IsPublic = true,
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
        };
        dbContext.PortfolioProfiles.Add(profile);
        await dbContext.SaveChangesAsync();

        var repository = new PortfolioProfileRepository(dbContext);
        var updated = await repository.SaveSocialLinksAsync(
            [
                new PortfolioSocialLink
                {
                    Id = profile.SocialLinks[0].Id,
                    Platform = "github",
                    Label = "GitHub Updated",
                    Url = "https://github.com/darkdhamon/updated",
                    SortOrder = 2,
                    IsVisible = false
                },
                new PortfolioSocialLink
                {
                    Platform = "dev",
                    Label = "DEV",
                    Url = "https://dev.to/example",
                    SortOrder = 1,
                    IsVisible = true
                }
            ]);

        Assert.That(updated, Has.Count.EqualTo(2));
        var updatedLinks = updated!;
        Assert.That(updatedLinks.OrderBy(link => link.SortOrder).Select(link => link.Label), Is.EqualTo(new[] { "DEV", "GitHub Updated" }));
        Assert.That(updatedLinks.Any(link => link.Label == "DEV"), Is.True);
        Assert.That(updatedLinks.Any(link => link.Label == "GitHub Updated"), Is.True);
    }

    [Test]
    public async Task SaveSocialLinksAsync_ReturnsNullWhenNoPublicProfileExists()
    {
        await using var dbContext = CreateDbContext();
        dbContext.PortfolioProfiles.Add(new PortfolioProfile
        {
            DisplayName = "Draft Profile",
            ContactHeadline = "Draft",
            ContactIntro = "Not public",
            IsPublic = false
        });
        await dbContext.SaveChangesAsync();

        var repository = new PortfolioProfileRepository(dbContext);
        var result = await repository.SaveSocialLinksAsync(
            [
                new PortfolioSocialLink
                {
                    Platform = "github",
                    Label = "GitHub",
                    Url = "https://github.com/darkdhamon",
                    IsVisible = true
                }
            ]);

        Assert.That(result, Is.Null);
        Assert.That(await dbContext.PortfolioSocialLinks.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task SaveSocialLinksAsync_DoesNotReturnDeletedLinks()
    {
        await using var dbContext = CreateDbContext();
        var profile = new PortfolioProfile
        {
            DisplayName = "Public Profile",
            ContactHeadline = "Contact updates",
            ContactIntro = "Contact section",
            IsPublic = true,
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
                    Platform = "linkedin",
                    Label = "LinkedIn",
                    Url = "https://linkedin.com/in/darkdhamon",
                    SortOrder = 2,
                    IsVisible = true
                }
            ]
        };
        dbContext.PortfolioProfiles.Add(profile);
        await dbContext.SaveChangesAsync();

        var repository = new PortfolioProfileRepository(dbContext);
        var saved = await repository.SaveSocialLinksAsync(
            [
                new PortfolioSocialLink
                {
                    Id = profile.SocialLinks[0].Id,
                    Platform = "github",
                    Label = "GitHub",
                    Url = "https://github.com/darkdhamon",
                    SortOrder = 1,
                    IsVisible = true
                }
            ]);

        Assert.That(saved, Has.Count.EqualTo(1));
        Assert.That(saved!.Single().Label, Is.EqualTo("GitHub"));
        Assert.That(await dbContext.PortfolioSocialLinks.CountAsync(), Is.EqualTo(1));
    }

    private static PortfolioDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new PortfolioDbContext(options);
    }
}
