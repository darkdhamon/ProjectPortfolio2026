using System.IO.Compression;
using System.Text;
using NUnit.Framework;
using ProjectPortfolio2026.ResumeParser.Implementations;

namespace ProjectPortfolio2026.ResumeParser.Tests;

[TestFixture]
public sealed class HeuristicResumeDocumentParserTests
{
    [Test]
    public async Task ParseAsync_TextResume_PopulatesSupportedSections()
    {
        var parser = new HeuristicResumeDocumentParser();
        var content = """
            Jane Example
            Senior Software Engineer
            jane@example.com | (312) 555-0100 | Chicago, IL | https://github.com/jane | https://www.linkedin.com/in/jane

            Professional Summary
            Product-minded engineer with a decade of experience building cloud software for internal and external users.

            Work Experience
            Senior Software Engineer at Example Corp | Jan 2022 - Present
            Chicago, IL
            - Led delivery of a resume import workflow used by recruiting operations.
            - Built .NET and React features across admin and public surfaces.
            Technologies: .NET, ASP.NET Core, React, SQL Server
            Skills: Leadership, Architecture

            Software Engineer
            Contoso LLC
            Feb 2019 - Dec 2021
            - Improved deployment automation and monitoring.
            Tools: Azure, Terraform, GitHub Actions

            Education
            Example State University
            Bachelor of Science in Computer Science
            2014 - 2018

            Skills
            Languages: C#, TypeScript, SQL
            Platforms: Azure, Docker

            Certifications
            Azure Developer Associate - Microsoft
            Credential ID: ABC-123
            2023

            Projects
            Resume Parser Refresh
            Jan 2024 - Mar 2024
            - Designed heuristics for work-history extraction.
            Stack: .NET, NUnit
            https://github.com/jane/resume-parser-refresh

            Languages
            English - Native, Spanish - Professional

            Awards
            Engineering Excellence Award
            Example Corp
            2023

            Volunteer Experience
            Mentor at Code Club
            2021 - Present
            - Coached students on web development fundamentals.

            Publications
            Modern Resume Parsing
            Tech Monthly
            2024
            https://example.com/publications/resume-parsing

            References
            Jordan Manager
            Director of Engineering
            jordan@example.com
            (773) 555-0111

            Community
            Speaker at local .NET meetups
            Organizer for hiring roundtables
            """;

        await using var stream = CreateTextStream(content);

        var document = await parser.ParseAsync(stream, "resume.txt");

        Assert.Multiple(() =>
        {
            Assert.That(document.Header.FullName, Is.EqualTo("Jane Example"));
            Assert.That(document.Header.EmailAddress, Is.EqualTo("jane@example.com"));
            Assert.That(document.Header.PhoneNumbers, Has.Count.EqualTo(1));
            Assert.That(document.Header.Profiles.Select(profile => profile.Label), Does.Contain("GitHub"));
            Assert.That(document.ProfessionalSummary, Does.Contain("Product-minded engineer"));
            Assert.That(document.WorkExperience, Has.Count.EqualTo(2));
            Assert.That(document.WorkExperience[0].EmployerName, Is.EqualTo("Example Corp"));
            Assert.That(document.WorkExperience[0].JobTitle, Is.EqualTo("Senior Software Engineer"));
            Assert.That(document.WorkExperience[0].EmploymentDates.IsCurrent, Is.True);
            Assert.That(document.WorkExperience[0].Technologies, Does.Contain(".NET"));
            Assert.That(document.WorkExperience[0].Skills, Does.Contain("Leadership"));
            Assert.That(document.Education[0].InstitutionName, Is.EqualTo("Example State University"));
            Assert.That(document.Skills.SelectMany(section => section.Items), Does.Contain("C#"));
            Assert.That(document.Certifications[0].Issuer, Is.EqualTo("Microsoft"));
            Assert.That(document.Projects[0].RepositoryUrl, Does.Contain("github.com/jane/resume-parser-refresh"));
            Assert.That(document.Languages.Select(language => language.Name), Does.Contain("English"));
            Assert.That(document.Awards[0].Title, Is.EqualTo("Engineering Excellence Award"));
            Assert.That(document.VolunteerExperience[0].OrganizationName, Is.EqualTo("Code Club"));
            Assert.That(document.Publications[0].Publisher, Is.EqualTo("Tech Monthly"));
            Assert.That(document.References[0].EmailAddress, Is.EqualTo("jordan@example.com"));
            Assert.That(document.AdditionalSections[0].Title, Is.EqualTo("Community"));
            Assert.That(document.ParserName, Is.EqualTo(nameof(HeuristicResumeDocumentParser)));
        });
    }

    [Test]
    public async Task ParseAsync_PartialResume_PreservesUsablePartialData()
    {
        var parser = new HeuristicResumeDocumentParser();
        var content = """
            Alex Candidate
            alex@example.com

            Work Experience
            Platform Engineer | Mar 2021 - Present
            - Improved CI stability for backend releases.
            - Mentored teammates during an infrastructure migration.

            Skills
            Kubernetes, Azure, Observability

            Interests
            Trail running
            Photography
            """;

        await using var stream = CreateTextStream(content);

        var document = await parser.ParseAsync(stream, "partial-resume.txt");

        Assert.Multiple(() =>
        {
            Assert.That(document.Header.FullName, Is.EqualTo("Alex Candidate"));
            Assert.That(document.WorkExperience, Has.Count.EqualTo(1));
            Assert.That(document.WorkExperience[0].JobTitle, Is.EqualTo("Platform Engineer"));
            Assert.That(document.WorkExperience[0].EmployerName, Is.Null);
            Assert.That(document.WorkExperience[0].DescriptionLines, Has.Count.EqualTo(2));
            Assert.That(document.Skills.SelectMany(section => section.Items), Does.Contain("Kubernetes"));
            Assert.That(document.AdditionalSections, Has.Count.EqualTo(1));
            Assert.That(document.AdditionalSections[0].Title, Is.EqualTo("Interests"));
        });
    }

    [Test]
    public async Task ParseAsync_DocxResume_ExtractsWordDocumentText()
    {
        var parser = new HeuristicResumeDocumentParser();
        await using var stream = CreateDocxStream(
        [
            "Jamie Resume",
            "jamie@example.com",
            "Work Experience",
            "Staff Engineer at Fabrikam | Jan 2020 - Present",
            "- Built reusable resume parsing heuristics."
        ]);

        var document = await parser.ParseAsync(stream, "resume.docx");

        Assert.Multiple(() =>
        {
            Assert.That(document.Header.FullName, Is.EqualTo("Jamie Resume"));
            Assert.That(document.WorkExperience, Has.Count.EqualTo(1));
            Assert.That(document.WorkExperience[0].EmployerName, Is.EqualTo("Fabrikam"));
            Assert.That(document.Metadata["contentType"], Does.Contain("officedocument"));
        });
    }

    private static MemoryStream CreateTextStream(string content)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }

    private static MemoryStream CreateDocxStream(IReadOnlyList<string> paragraphs)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var contentTypes = archive.CreateEntry("[Content_Types].xml");
            using (var contentTypeWriter = new StreamWriter(contentTypes.Open(), Encoding.UTF8, leaveOpen: false))
            {
                contentTypeWriter.Write("""
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                      <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                      <Default Extension="xml" ContentType="application/xml"/>
                      <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
                    </Types>
                    """);
            }

            archive.CreateEntry("_rels/.rels");
            var documentEntry = archive.CreateEntry("word/document.xml");
            using var writer = new StreamWriter(documentEntry.Open(), Encoding.UTF8, leaveOpen: false);
            writer.Write("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>""");
            foreach (var paragraph in paragraphs)
            {
                writer.Write($"""<w:p><w:r><w:t>{System.Security.SecurityElement.Escape(paragraph)}</w:t></w:r></w:p>""");
            }

            writer.Write("""</w:body></w:document>""");
        }

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }
}
