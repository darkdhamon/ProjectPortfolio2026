using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

using ProjectPortfolio2026.Server.Data;
using ProjectPortfolio2026.Server.Domain.Tags;
using ProjectPortfolio2026.Server.Domain.WorkHistory;
using ProjectPortfolio2026.Server.Repositories;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class EmployerRepositoryTests
{
    [Test]
    public async Task ListPublishedAsync_ReturnsPublishedEmployers_WithOrderedRolesAndCategorizedTags()
    {
        await using var dbContext = CreateDbContext();
        var repository = new EmployerRepository(dbContext);

        dbContext.Employers.AddRange(
            new Employer
            {
                Name = "Northwind Health",
                City = "Chicago",
                Region = "IL",
                IsPublished = true,
                JobRoles =
                [
                    new JobRole
                    {
                        Role = "Senior Developer",
                        StartDate = new DateOnly(2024, 1, 1),
                        EndDate = null,
                        SupervisorName = "Dana Smith",
                        DescriptionMarkdown = "Current role.",
                        JobRoleTags =
                        [
                            CreateJobRoleTag(TagCategory.Skill, "API Design"),
                            CreateJobRoleTag(TagCategory.Technology, ".NET")
                        ]
                    },
                    new JobRole
                    {
                        Role = "Developer",
                        StartDate = new DateOnly(2022, 1, 1),
                        EndDate = new DateOnly(2023, 12, 31),
                        DescriptionMarkdown = "Previous role."
                    }
                ]
            },
            new Employer
            {
                Name = "Hidden Employer",
                IsPublished = false,
                JobRoles =
                [
                    new JobRole
                    {
                        Role = "Hidden Role",
                        StartDate = new DateOnly(2020, 1, 1),
                        DescriptionMarkdown = "Should not appear."
                    }
                ]
            },
            new Employer
            {
                Name = "Blue Ocean Labs",
                City = "Austin",
                Region = "TX",
                IsPublished = true,
                JobRoles =
                [
                    new JobRole
                    {
                        Role = "Platform Engineer",
                        StartDate = new DateOnly(2021, 5, 1),
                        EndDate = new DateOnly(2023, 6, 1),
                        DescriptionMarkdown = "Past role."
                    }
                ]
            });

        await dbContext.SaveChangesAsync();

        var employers = await repository.ListPublishedAsync();

        Assert.That(employers, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(employers.Select(employer => employer.Name), Is.EqualTo(new[] { "Northwind Health", "Blue Ocean Labs" }));
            Assert.That(employers[0].JobRoles.Select(jobRole => jobRole.Role), Is.EqualTo(new[] { "Senior Developer", "Developer" }));
            Assert.That(employers[0].JobRoles[0].JobRoleTags.Where(jobRoleTag => jobRoleTag.Tag!.Category == TagCategory.Skill).Select(jobRoleTag => jobRoleTag.Tag!.DisplayName), Is.EqualTo(new[] { "API Design" }));
            Assert.That(employers[0].JobRoles[0].JobRoleTags.Where(jobRoleTag => jobRoleTag.Tag!.Category == TagCategory.Technology).Select(jobRoleTag => jobRoleTag.Tag!.DisplayName), Is.EqualTo(new[] { ".NET" }));
        });
    }

    private static PortfolioDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new PortfolioDbContext(options);
    }

    private static JobRoleTag CreateJobRoleTag(TagCategory category, string displayName)
    {
        return new JobRoleTag
        {
            Tag = new Tag
            {
                Category = category,
                DisplayName = displayName,
                NormalizedName = displayName.Trim().ToUpperInvariant()
            }
        };
    }
}
