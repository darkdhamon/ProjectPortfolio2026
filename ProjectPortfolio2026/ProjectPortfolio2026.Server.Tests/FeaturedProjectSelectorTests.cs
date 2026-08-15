using NUnit.Framework;
using ProjectPortfolio2026.Server.Contracts.Projects;
using ProjectPortfolio2026.Server.Services.Implementations;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class FeaturedProjectSelectorTests
{
    [Test]
    public void Select_PrefersFeaturedProjectsAndFillsRemainingSlots()
    {
        var selector = new FeaturedProjectSelector();
        var publishedProjects = new List<ProjectListItem>
        {
            new() { Id = 1, Title = "Featured Alpha", IsFeatured = true },
            new() { Id = 2, Title = "Featured Beta", IsFeatured = true },
            new() { Id = 3, Title = "Recent Gamma" },
            new() { Id = 4, Title = "Recent Delta" },
        };

        var result = selector.Select(publishedProjects, 3);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(3));
            Assert.That(result.Select(item => item.Id), Is.EqualTo([1, 2, 3]));
            Assert.That(result[2].IsFeatured, Is.False);
        });
    }

    [Test]
    public void Select_ClampsLimitToOneThroughFiveAndDoesNotDuplicateProjects()
    {
        var selector = new FeaturedProjectSelector();
        var tooFewPublished = new List<ProjectListItem>
        {
            new() { Id = 1, IsFeatured = true },
            new() { Id = 2, IsFeatured = true },
            new() { Id = 3 }
        };
        var overLimit = new List<ProjectListItem>
        {
            new() { Id = 1, IsFeatured = true },
            new() { Id = 2, IsFeatured = true },
            new() { Id = 3, IsFeatured = true },
            new() { Id = 4, IsFeatured = true },
            new() { Id = 5, IsFeatured = true },
            new() { Id = 6, IsFeatured = true }
        };

        var singleResult = selector.Select(tooFewPublished, 0);
        var cappedResult = selector.Select(overLimit, 10);

        Assert.Multiple(() =>
        {
            Assert.That(singleResult, Has.Count.EqualTo(1));
            Assert.That(cappedResult, Has.Count.EqualTo(5));
            Assert.That(cappedResult.Select(item => item.Id).Distinct().Count(), Is.EqualTo(5));
        });
    }

    [Test]
    public void Select_WhenLimitSmallerThanFeaturedCount_ReturnsOnlyFeaturedProjects()
    {
        var selector = new FeaturedProjectSelector();
        var publishedProjects = new List<ProjectListItem>
        {
            new() { Id = 1, IsFeatured = true },
            new() { Id = 2, IsFeatured = true },
            new() { Id = 3, IsFeatured = true },
            new() { Id = 4, IsFeatured = true }
        };

        var result = selector.Select(publishedProjects, 2);

        Assert.That(result, Has.All.Matches<ProjectListItem>(project => project.IsFeatured));
        Assert.That(result.Select(project => project.Id), Has.All.InRange(1, 4));
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void Select_OrdersFeaturedProjectsByFeaturedOrderAndFallbacksToStartDateTitle()
    {
        var selector = new FeaturedProjectSelector();
        var publishedProjects = new List<ProjectListItem>
        {
            new() { Id = 1, Title = "NoRankLater", IsFeatured = true, StartDate = new DateOnly(2026, 2, 1) },
            new() { Id = 2, Title = "TopRank", IsFeatured = true, FeaturedOrder = 0, StartDate = new DateOnly(2025, 1, 1) },
            new() { Id = 3, Title = "NoRankLatest", IsFeatured = true, StartDate = new DateOnly(2026, 8, 1) },
            new() { Id = 4, Title = "SecondRank", IsFeatured = true, FeaturedOrder = 1, StartDate = new DateOnly(2026, 1, 1) },
            new() { Id = 5, Title = "SecondLatestTiebreak", IsFeatured = true, StartDate = new DateOnly(2026, 8, 1) }
        };

        var result = selector.Select(publishedProjects, 5);

        Assert.That(result.Select(item => item.Id), Is.EqualTo(new[] { 2, 4, 3, 5, 1 }));
    }
}
