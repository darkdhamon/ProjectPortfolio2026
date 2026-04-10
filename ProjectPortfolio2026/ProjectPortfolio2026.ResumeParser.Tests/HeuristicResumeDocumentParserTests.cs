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
    public async Task ParseAsync_UnicodeBulletResume_RecognizesBulletDescriptionsAndDelimitedSkills()
    {
        var parser = new HeuristicResumeDocumentParser();
        var content = """
            Taylor Example
            taylor@example.com

            Work Experience
            Senior Engineer at Northwind
            Jan 2022 – Present
            • Led delivery of a portfolio refresh.
            • Partnered with design and recruiting teams.
            Technologies: C# • SQL • Azure

            Skills
            Platforms: Azure • GitHub Actions • Docker
            """;

        await using var stream = CreateTextStream(content);

        var document = await parser.ParseAsync(stream, "unicode-bullets.txt");

        Assert.Multiple(() =>
        {
            Assert.That(document.WorkExperience, Has.Count.EqualTo(1));
            Assert.That(document.WorkExperience[0].DescriptionLines, Has.Count.EqualTo(2));
            Assert.That(document.WorkExperience[0].Technologies, Does.Contain("Azure"));
            Assert.That(document.Skills.SelectMany(section => section.Items), Does.Contain("GitHub Actions"));
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

    [Test]
    public async Task ParseAsync_PdfResume_ExtractsPdfText()
    {
        var parser = new HeuristicResumeDocumentParser();
        await using var stream = CreatePdfStream(
        [
            "Morgan Resume",
            "morgan@example.com",
            "Work Experience",
            "Principal Engineer at Adventure Works",
            "Jan 2021 - Present"
        ]);

        var document = await parser.ParseAsync(stream, "resume.pdf");

        Assert.Multiple(() =>
        {
            Assert.That(document.Metadata["contentType"], Is.EqualTo("application/pdf"));
            Assert.That(document.RawText, Does.Not.Contain("%PDF"));
        });
    }

    [Test]
    public void ParseAsync_UnsupportedBinaryFormat_ThrowsNotSupportedException()
    {
        var parser = new HeuristicResumeDocumentParser();
        using var stream = new MemoryStream([0x50, 0x4B, 0x03, 0x04, 0x00, 0x01, 0x02, 0x03]);

        var act = async () => await parser.ParseAsync(stream, "resume.bin");

        Assert.That(act, Throws.TypeOf<NotSupportedException>());
    }

    [Test]
    public void ParseAsync_HighByteBinaryFormat_ThrowsNotSupportedException()
    {
        var parser = new HeuristicResumeDocumentParser();
        using var stream = new MemoryStream([0xFF, 0xD8, 0xE0, 0xAA, 0xFE, 0xF1, 0xC0, 0xAF]);

        var act = async () => await parser.ParseAsync(stream, "resume.dat");

        Assert.That(act, Throws.TypeOf<NotSupportedException>());
    }

    [Test]
    public void ParseAsync_RtfFile_ThrowsNotSupportedExceptionUntilRtfExtractionExists()
    {
        var parser = new HeuristicResumeDocumentParser();
        using var stream = CreateTextStream(@"{\rtf1\ansi Jane Example\par Senior Engineer}");

        var act = async () => await parser.ParseAsync(stream, "resume.rtf");

        Assert.That(act, Throws.TypeOf<NotSupportedException>());
    }

    [Test]
    public async Task ParseAsync_MultiWordCustomSection_PreservesAdditionalSection()
    {
        var parser = new HeuristicResumeDocumentParser();
        var content = """
            Riley Example
            riley@example.com

            Work Experience
            Staff Engineer at Fabrikam
            2020 - Present
            - Led API modernization work.

            Open Source Contributions
            Maintainer for internal NuGet packages
            Speaker at .NET user groups
            """;

        await using var stream = CreateTextStream(content);

        var document = await parser.ParseAsync(stream, "custom-sections.txt");

        Assert.Multiple(() =>
        {
            Assert.That(document.AdditionalSections, Has.Count.EqualTo(1));
            Assert.That(document.AdditionalSections[0].Title, Is.EqualTo("Open Source Contributions"));
            Assert.That(document.AdditionalSections[0].Items, Does.Contain("Maintainer for internal NuGet packages"));
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

    private static MemoryStream CreatePdfStream(IReadOnlyList<string> lines)
    {
        static string EscapePdfText(string value) => value.Replace(@"\", @"\\").Replace("(", @"\(").Replace(")", @"\)");

        var contentBuilder = new StringBuilder();
        contentBuilder.AppendLine("BT");
        contentBuilder.AppendLine("/F1 12 Tf");
        contentBuilder.AppendLine("72 720 Td");
        for (var index = 0; index < lines.Count; index++)
        {
            if (index > 0)
            {
                contentBuilder.AppendLine("T*");
            }

            contentBuilder.Append('(').Append(EscapePdfText(lines[index])).AppendLine(") Tj");
        }

        contentBuilder.AppendLine("ET");

        var streamContent = contentBuilder.ToString();
        var objects = new[]
        {
            "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj",
            "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj",
            "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >> endobj",
            $"4 0 obj << /Length {Encoding.ASCII.GetByteCount(streamContent)} >> stream\n{streamContent}endstream\nendobj",
            "5 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj"
        };

        using var writerStream = new MemoryStream();
        using (var writer = new StreamWriter(writerStream, Encoding.ASCII, leaveOpen: true))
        {
            writer.NewLine = "\n";
            writer.WriteLine("%PDF-1.4");
            writer.Flush();
            var offsets = new List<long> { 0 };
            foreach (var obj in objects)
            {
                offsets.Add(writerStream.Position);
                writer.WriteLine(obj);
                writer.Flush();
            }

            var xrefOffset = writerStream.Position;
            writer.WriteLine($"xref\n0 {objects.Length + 1}");
            writer.WriteLine("0000000000 65535 f ");
            foreach (var offset in offsets.Skip(1))
            {
                writer.WriteLine($"{offset:D10} 00000 n ");
            }

            writer.WriteLine($"trailer << /Size {objects.Length + 1} /Root 1 0 R >>");
            writer.WriteLine("startxref");
            writer.WriteLine(xrefOffset.ToString());
            writer.Write("%%EOF");
            writer.Flush();
        }

        return new MemoryStream(writerStream.ToArray());
    }
}
