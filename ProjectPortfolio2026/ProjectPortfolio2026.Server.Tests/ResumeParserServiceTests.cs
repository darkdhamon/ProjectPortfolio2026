using NUnit.Framework;
using ProjectPortfolio2026.ResumeParser.Interfaces;
using ProjectPortfolio2026.ResumeParser.Models;
using ProjectPortfolio2026.Server.Services.Implementations;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class ResumeParserServiceTests
{
    [Test]
    public async Task ParseAsync_MapsFullResumeDocumentToAppSpecificImportResult()
    {
        var parser = new StubResumeDocumentParser
        {
            Result = new ResumeDocument
            {
                Header = new ResumeHeader
                {
                    FullName = "Bronze Loft",
                    EmailAddress = "bronze@example.dev",
                    PhoneNumbers = ["312-555-0147"],
                    Location = new ResumeLocation
                    {
                        City = "Chicago",
                        Region = "IL",
                        Country = "USA",
                        DisplayText = "Chicago, IL"
                    },
                    Profiles =
                    [
                        new ResumeProfileLink { Url = "https://github.com/darkdhamon" },
                        new ResumeProfileLink { Label = "LinkedIn" }
                    ]
                },
                ProfessionalSummary = "Experienced engineer",
                SourceFileName = "resume.pdf",
                ParserName = "StubParser",
                RawText = "raw resume text",
                Skills =
                [
                    new ResumeSkillSection { Name = "Technologies", Items = ["C#", ".NET", "C#"] },
                    new ResumeSkillSection { Name = "Practices", Items = ["API Design"] }
                ],
                WorkExperience =
                [
                    new ResumeWorkExperienceEntry
                    {
                        EmployerName = "Northwind Health",
                        JobTitle = "Senior Software Engineer",
                        EmploymentType = "Full-time",
                        SupervisorName = "Dana Smith",
                        EmploymentDates = new ResumeDateRange
                        {
                            StartDateText = "Jan 2024",
                            EndDateText = "Present",
                            StartDate = new DateOnly(2024, 1, 1),
                            IsCurrent = true
                        },
                        DescriptionLines = ["Built APIs", "Led refactors"],
                        DescriptionMarkdown = "- Built APIs\n- Led refactors",
                        Skills = ["API Design"],
                        Technologies = [".NET 10"],
                        Tags = ["backend"],
                        RawText = "Northwind Health ...",
                        Metadata = new Dictionary<string, string?>
                        {
                            ["source-section"] = "experience"
                        }
                    }
                ]
            }
        };
        var service = new ResumeParserService(parser);

        using var stream = new MemoryStream([1, 2, 3]);

        var result = await service.ParseAsync(stream, "resume.pdf");

        Assert.Multiple(() =>
        {
            Assert.That(result.Person?.FullName, Is.EqualTo("Bronze Loft"));
            Assert.That(result.Person?.SocialProfiles, Is.EqualTo(new[] { "https://github.com/darkdhamon", "LinkedIn" }));
            Assert.That(result.ProfessionalSummary, Is.EqualTo("Experienced engineer"));
            Assert.That(result.GlobalSkills, Is.EqualTo(new[] { ".NET", "API Design", "C#" }));
            Assert.That(result.WorkHistory, Has.Count.EqualTo(1));
            Assert.That(result.WorkHistory[0].EmployerName, Is.EqualTo("Northwind Health"));
            Assert.That(result.WorkHistory[0].EmploymentDates.IsCurrentRole, Is.True);
            Assert.That(result.WorkHistory[0].RawFields["source-section"], Is.EqualTo("experience"));
        });
    }

    private sealed class StubResumeDocumentParser : IResumeDocumentParser
    {
        public ResumeDocument Result { get; set; } = new();

        public Task<ResumeDocument> ParseAsync(
            Stream content,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result);
        }
    }
}
