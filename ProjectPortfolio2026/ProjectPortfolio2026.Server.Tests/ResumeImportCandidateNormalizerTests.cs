using NUnit.Framework;
using ProjectPortfolio2026.Server.Services.Implementations;
using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class ResumeImportCandidateNormalizerTests
{
    [Test]
    public void Normalize_GroupsRolesByEmployerAndPreservesStructuredCandidates()
    {
        var result = ResumeImportCandidateNormalizer.Normalize(new ResumeImportParseResult
        {
            WorkHistory =
            [
                new ParsedWorkHistoryEntry
                {
                    EmployerName = " Northwind Health ",
                    EmployerLocation = new ParsedLocation
                    {
                        City = " Chicago ",
                        Region = " IL "
                    },
                    JobTitle = " Senior Software Engineer ",
                    EmploymentDates = new ParsedDateRange
                    {
                        StartDateText = "Jan 2020",
                        StartDate = new DateOnly(2020, 1, 1)
                    },
                    DescriptionLines = [" Built ATS export tooling ", " Built ATS export tooling "],
                    Skills = [" C# ", "c#"],
                    Technologies = [".NET", " .NET "]
                },
                new ParsedWorkHistoryEntry
                {
                    EmployerName = "Northwind Health",
                    EmployerLocation = new ParsedLocation
                    {
                        City = "Chicago",
                        Region = "IL"
                    },
                    JobTitle = "Staff Engineer",
                    DescriptionMarkdown = " Led platform modernization. "
                }
            ],
            GlobalSkills = [" .NET ", "c#", "C#"]
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.CandidateWorkHistory, Has.Count.EqualTo(1));
            Assert.That(result.CandidateWorkHistory[0].CandidateId, Is.EqualTo("employer-001"));
            Assert.That(result.CandidateWorkHistory[0].EmployerName, Is.EqualTo("Northwind Health"));
            Assert.That(result.CandidateWorkHistory[0].EmployerLocation?.City, Is.EqualTo("Chicago"));
            Assert.That(result.CandidateWorkHistory[0].JobRoles, Has.Count.EqualTo(2));
            Assert.That(result.CandidateWorkHistory[0].JobRoles[0].CandidateId, Is.EqualTo("role-001"));
            Assert.That(result.CandidateWorkHistory[0].JobRoles[0].DescriptionLines, Is.EqualTo(new[] { "Built ATS export tooling" }));
            Assert.That(result.CandidateWorkHistory[0].JobRoles[0].DescriptionMarkdown, Is.EqualTo("Built ATS export tooling"));
            Assert.That(result.CandidateWorkHistory[0].JobRoles[0].Skills, Is.EqualTo(new[] { "C#" }));
            Assert.That(result.CandidateWorkHistory[0].JobRoles[0].Technologies, Is.EqualTo(new[] { ".NET" }));
            Assert.That(result.CandidateWorkHistory[0].JobRoles[1].DescriptionMarkdown, Is.EqualTo("Led platform modernization."));
            Assert.That(result.GlobalSkills, Is.EqualTo(new[] { ".NET", "c#" }));
        });
    }

    [Test]
    public void Normalize_KeepsPartialEntriesWithoutMergingUnnamedEmployers()
    {
        var result = ResumeImportCandidateNormalizer.Normalize(new ResumeImportParseResult
        {
            WorkHistory =
            [
                new ParsedWorkHistoryEntry
                {
                    JobTitle = "Contractor",
                    DescriptionLines = ["Shipped production fixes"]
                },
                new ParsedWorkHistoryEntry
                {
                    JobTitle = "Consultant",
                    DescriptionLines = ["Provided delivery support"]
                }
            ]
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.CandidateWorkHistory, Has.Count.EqualTo(2));
            Assert.That(result.CandidateWorkHistory[0].JobRoles[0].JobTitle, Is.EqualTo("Contractor"));
            Assert.That(result.CandidateWorkHistory[1].JobRoles[0].JobTitle, Is.EqualTo("Consultant"));
        });
    }

    [Test]
    public void Normalize_TrimsDuplicateRawFieldKeysWithoutThrowingAndKeepsStructuredValue()
    {
        var result = ResumeImportCandidateNormalizer.Normalize(new ResumeImportParseResult
        {
            WorkHistory =
            [
                new ParsedWorkHistoryEntry
                {
                    EmployerName = "Northwind Health",
                    JobTitle = "Engineer",
                    RawFields = new Dictionary<string, string?>(StringComparer.Ordinal)
                    {
                        [" source-section "] = "   ",
                        ["source-section"] = " experience ",
                        ["detail"] = " imported "
                    }
                }
            ]
        });

        var rawFields = result.CandidateWorkHistory[0].JobRoles[0].RawFields;

        Assert.Multiple(() =>
        {
            Assert.That(rawFields, Has.Count.EqualTo(2));
            Assert.That(rawFields["source-section"], Is.EqualTo("experience"));
            Assert.That(rawFields["detail"], Is.EqualTo("imported"));
        });
    }

    [Test]
    public void Normalize_CollapsesWhitespaceOnlyStructuredValuesAndPreservesExistingRawFieldValues()
    {
        var result = ResumeImportCandidateNormalizer.Normalize(new ResumeImportParseResult
        {
            Person = new ParsedPerson
            {
                FullName = "  Taylor Jordan  ",
                PhoneNumbers = ["  ", " 555-0100 ", "555-0100"],
                Location = new ParsedLocation
                {
                    City = " ",
                    Region = "\t"
                },
                SocialProfiles = ["  https://linkedin.example/taylor  ", "https://linkedin.example/taylor"]
            },
            ProfessionalSummary = "  ",
            RawText = "\r\n imported raw text \r\n",
            SourceFileName = " resume.pdf ",
            ParserName = " parser-x ",
            WorkHistory =
            [
                new ParsedWorkHistoryEntry
                {
                    EmployerName = "  Fabrikam  ",
                    EmployerLocation = new ParsedLocation
                    {
                        DisplayText = " "
                    },
                    JobTitle = " Engineer ",
                    EmploymentDates = null!,
                    DescriptionMarkdown = " ",
                    DescriptionLines = ["  ", "\t"],
                    RawRoleText = " ",
                    RawFields = new Dictionary<string, string?>(StringComparer.Ordinal)
                    {
                        ["detail"] = " first value ",
                        [" detail "] = " second value ",
                        ["empty"] = " "
                    }
                }
            ]
        });

        var candidate = result.CandidateWorkHistory[0];
        var role = candidate.JobRoles[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Person, Is.Not.Null);
            Assert.That(result.Person?.FullName, Is.EqualTo("Taylor Jordan"));
            Assert.That(result.Person?.PhoneNumbers, Is.EqualTo(new[] { "555-0100" }));
            Assert.That(result.Person?.Location, Is.Null);
            Assert.That(result.Person?.SocialProfiles, Is.EqualTo(new[] { "https://linkedin.example/taylor" }));
            Assert.That(result.ProfessionalSummary, Is.Null);
            Assert.That(result.RawText, Is.EqualTo("imported raw text"));
            Assert.That(result.SourceFileName, Is.EqualTo("resume.pdf"));
            Assert.That(result.ParserName, Is.EqualTo("parser-x"));
            Assert.That(candidate.EmployerName, Is.EqualTo("Fabrikam"));
            Assert.That(candidate.EmployerLocation, Is.Null);
            Assert.That(role.EmploymentDates, Is.Not.Null);
            Assert.That(role.EmploymentDates.StartDateText, Is.Null);
            Assert.That(role.EmploymentDates.EndDateText, Is.Null);
            Assert.That(role.DescriptionLines, Is.Empty);
            Assert.That(role.DescriptionMarkdown, Is.Null);
            Assert.That(role.RawRoleText, Is.Null);
            Assert.That(role.RawFields["detail"], Is.EqualTo("first value"));
            Assert.That(role.RawFields["empty"], Is.Null);
        });
    }
}
