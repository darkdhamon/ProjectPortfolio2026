using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ProjectPortfolio2026.ResumeParser.Interfaces;
using ProjectPortfolio2026.ResumeParser.Models;
using UglyToad.PdfPig;

namespace ProjectPortfolio2026.ResumeParser.Implementations;

public sealed partial class HeuristicResumeDocumentParser : IResumeDocumentParser
{
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt",
        ".md"
    };

    private static readonly HashSet<string> CustomHeadingKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "activities",
        "affiliations",
        "contributions",
        "experience",
        "interests",
        "involvement",
        "leadership",
        "organizations",
        "service"
    };

    private static readonly string[] HeaderSectionOrder =
    [
        "summary",
        "experience",
        "education",
        "skills",
        "certifications",
        "projects",
        "languages",
        "awards",
        "volunteer",
        "publications",
        "references"
    ];

    private static readonly Dictionary<string, string> SectionAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["professional summary"] = "summary",
        ["summary"] = "summary",
        ["profile"] = "summary",
        ["objective"] = "summary",
        ["work experience"] = "experience",
        ["experience"] = "experience",
        ["professional experience"] = "experience",
        ["employment history"] = "experience",
        ["work history"] = "experience",
        ["career history"] = "experience",
        ["education"] = "education",
        ["academic background"] = "education",
        ["skills"] = "skills",
        ["technical skills"] = "skills",
        ["core competencies"] = "skills",
        ["competencies"] = "skills",
        ["certifications"] = "certifications",
        ["licenses"] = "certifications",
        ["projects"] = "projects",
        ["selected projects"] = "projects",
        ["languages"] = "languages",
        ["awards"] = "awards",
        ["honors"] = "awards",
        ["honors & awards"] = "awards",
        ["volunteer experience"] = "volunteer",
        ["volunteering"] = "volunteer",
        ["publications"] = "publications",
        ["references"] = "references"
    };

    public async Task<ResumeDocument> ParseAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("A source file name is required for resume parsing.", nameof(fileName));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var rawText = await ExtractTextAsync(content, fileName, cancellationToken);
        var normalizedText = NormalizeText(rawText);
        var lines = normalizedText.Split('\n').Select(static line => line.TrimEnd()).ToList();
        var sections = BuildSections(lines);

        var headerLines = sections.TryGetValue("__header__", out var discoveredHeaderLines)
            ? discoveredHeaderLines
            : [];

        sections.TryGetValue("summary", out var summaryLines);
        sections.TryGetValue("experience", out var experienceLines);
        sections.TryGetValue("education", out var educationLines);
        sections.TryGetValue("skills", out var skillLines);
        sections.TryGetValue("certifications", out var certificationLines);
        sections.TryGetValue("projects", out var projectLines);
        sections.TryGetValue("languages", out var languageLines);
        sections.TryGetValue("awards", out var awardLines);
        sections.TryGetValue("volunteer", out var volunteerLines);
        sections.TryGetValue("publications", out var publicationLines);
        sections.TryGetValue("references", out var referenceLines);

        return new ResumeDocument
        {
            Header = ParseHeader(headerLines),
            ProfessionalSummary = JoinSectionParagraph(summaryLines),
            WorkExperience = ParseWorkExperience(experienceLines),
            Education = ParseEducation(educationLines),
            Skills = ParseSkillSections(skillLines),
            Certifications = ParseCertifications(certificationLines),
            Projects = ParseProjects(projectLines),
            Languages = ParseLanguages(languageLines),
            Awards = ParseAwards(awardLines),
            VolunteerExperience = ParseVolunteerExperience(volunteerLines),
            Publications = ParsePublications(publicationLines),
            References = ParseReferences(referenceLines),
            AdditionalSections = ParseAdditionalSections(sections),
            RawText = normalizedText,
            SourceFileName = fileName,
            ParserName = nameof(HeuristicResumeDocumentParser),
            Metadata = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["contentType"] = GetContentTypeFromFileName(fileName),
                ["parsedAtUtc"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            }
        };
    }

    private static async Task<string> ExtractTextAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken)
    {
        if (content.CanSeek)
        {
            content.Seek(0, SeekOrigin.Begin);
        }

        var extension = Path.GetExtension(fileName);
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Seek(0, SeekOrigin.Begin);

        if (extension.Equals(".docx", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractDocxText(buffer);
        }

        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractPdfText(buffer);
        }

        if (TextExtensions.Contains(extension))
        {
            return ReadTextBuffer(buffer);
        }

        if (string.IsNullOrEmpty(extension) && LooksLikeUtf8TextContent(buffer))
        {
            return ReadTextBuffer(buffer);
        }

        throw new NotSupportedException(
            $"The resume parser does not support '{extension}' files yet. Supported formats currently include .docx, .pdf, .txt, and .md.");
    }

    private static string ExtractPdfText(Stream content)
    {
        byte[] rawBytes;
        if (content.CanSeek)
        {
            content.Seek(0, SeekOrigin.Begin);
        }

        if (content is MemoryStream memoryStream)
        {
            rawBytes = memoryStream.ToArray();
        }
        else
        {
            using var capture = new MemoryStream();
            content.CopyTo(capture);
            rawBytes = capture.ToArray();
            content.Seek(0, SeekOrigin.Begin);
        }

        using var document = PdfDocument.Open(content);
        var pageText = document.GetPages()
            .Select(page => page.Text)
            .Where(static text => !string.IsNullOrWhiteSpace(text))
            .ToList();

        if (pageText.Count > 0)
        {
            return string.Join(Environment.NewLine + Environment.NewLine, pageText);
        }

        var rawPdf = Encoding.ASCII.GetString(rawBytes);
        var matches = PdfLiteralTextRegex().Matches(rawPdf);
        var extracted = matches
            .Select(match => Regex.Unescape(match.Groups["text"].Value))
            .Where(static text => !string.IsNullOrWhiteSpace(text))
            .ToList();

        return string.Join(Environment.NewLine, extracted);
    }

    private static bool LooksLikeUtf8TextContent(MemoryStream content)
    {
        var buffer = content.ToArray();
        if (buffer.Length == 0)
        {
            return true;
        }

        try
        {
            var strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            var text = strictUtf8.GetString(buffer);
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var sampleLength = Math.Min(text.Length, 4096);
            var textSample = text[..sampleLength];
            var letterCount = textSample.Count(char.IsLetter);
            var disallowedControlCount = textSample.Count(character => char.IsControl(character) &&
                                                                       character is not '\r' and not '\n' and not '\t');

            return letterCount > 0 && disallowedControlCount == 0;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static string ExtractDocxText(Stream content)
    {
        using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.GetEntry("word/document.xml");
        if (entry is null)
        {
            return string.Empty;
        }

        using var entryStream = entry.Open();
        var document = XDocument.Load(entryStream);
        XNamespace wordNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var paragraphTexts = document
            .Descendants(wordNamespace + "p")
            .Select(paragraph => string.Concat(paragraph.Descendants(wordNamespace + "t").Select(node => node.Value)))
            .ToList();

        return string.Join(Environment.NewLine, paragraphTexts);
    }

    private static string ReadTextBuffer(MemoryStream content)
    {
        content.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = reader.ReadToEnd();
        content.Seek(0, SeekOrigin.Begin);

        return text.IndexOf('\0') >= 0
            ? text.Replace("\0", string.Empty, StringComparison.Ordinal)
            : text;
    }

    private static string NormalizeText(string text)
    {
        var normalized = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace('\u00A0', ' ')
            .Replace("\t", " ", StringComparison.Ordinal);

        normalized = MultipleSpaceRegex().Replace(normalized, " ");
        normalized = MultipleBlankLineRegex().Replace(normalized, "\n\n");
        return normalized.Trim();
    }

    private static Dictionary<string, List<string>> BuildSections(IReadOnlyList<string> lines)
    {
        var sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["__header__"] = []
        };

        var currentSection = "__header__";
        var discoveredKnownSection = false;
        var previousLineWasBlank = true;

        foreach (var line in lines)
        {
            var isBlankLine = string.IsNullOrWhiteSpace(line);
            if (TryNormalizeSectionHeading(line, out var normalizedSection, out var originalHeading))
            {
                if (normalizedSection.StartsWith("custom:", StringComparison.OrdinalIgnoreCase) &&
                    (!discoveredKnownSection || !previousLineWasBlank))
                {
                    // Keep the resume header intact until we hit a known resume section.
                }
                else
                {
                    currentSection = normalizedSection;
                    if (!sections.ContainsKey(currentSection))
                    {
                        sections[currentSection] = [];
                    }

                    if (!HeaderSectionOrder.Contains(currentSection, StringComparer.OrdinalIgnoreCase))
                    {
                        sections[currentSection].Add(originalHeading);
                    }

                    if (!normalizedSection.StartsWith("custom:", StringComparison.OrdinalIgnoreCase))
                    {
                        discoveredKnownSection = true;
                    }

                    previousLineWasBlank = false;
                    continue;
                }
            }

            if (!sections.TryGetValue(currentSection, out var sectionLines))
            {
                sectionLines = [];
                sections[currentSection] = sectionLines;
            }

            sectionLines.Add(line);
            previousLineWasBlank = isBlankLine;
        }

        return sections;
    }

    private static bool TryNormalizeSectionHeading(string line, out string normalizedSection, out string originalHeading)
    {
        originalHeading = line.Trim().Trim(':').Trim();
        normalizedSection = string.Empty;

        if (string.IsNullOrWhiteSpace(originalHeading) || originalHeading.Length > 40)
        {
            return false;
        }

        var candidate = CollapseWhitespace(originalHeading).ToLowerInvariant();
        if (SectionAliases.TryGetValue(candidate, out var alias))
        {
            normalizedSection = alias;
            return true;
        }

        if (candidate.EndsWith(" experience", StringComparison.OrdinalIgnoreCase) &&
            !candidate.StartsWith("work", StringComparison.OrdinalIgnoreCase) &&
            !candidate.StartsWith("professional", StringComparison.OrdinalIgnoreCase) &&
            !candidate.StartsWith("volunteer", StringComparison.OrdinalIgnoreCase))
        {
            normalizedSection = candidate;
            return true;
        }

        if (IsLikelyCustomHeading(originalHeading))
        {
            normalizedSection = "custom:" + candidate;
            return true;
        }

        return false;
    }

    private static ResumeHeader ParseHeader(IReadOnlyList<string>? rawLines)
    {
        var lines = CleanSectionLines(rawLines);
        var header = new ResumeHeader();
        if (lines.Count == 0)
        {
            return header;
        }

        var fullName = lines
            .FirstOrDefault(line => !LooksLikeContactLine(line) && !line.Contains('|') && !line.Contains('@'));
        var headline = lines
            .SkipWhile(line => line != fullName)
            .Skip(1)
            .FirstOrDefault(line => !LooksLikeContactLine(line));

        var email = lines.Select(ExtractEmailAddress).FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));
        var phoneNumbers = lines.SelectMany(ExtractPhoneNumbers).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var profiles = lines.SelectMany(ExtractProfileLinks).ToList();
        var locationLine = lines.FirstOrDefault(MightBeLocation);
        var nameParts = SplitFullName(fullName);

        return new ResumeHeader
        {
            FullName = fullName,
            FirstName = nameParts.firstName,
            MiddleName = nameParts.middleName,
            LastName = nameParts.lastName,
            Headline = headline,
            EmailAddress = email,
            PhoneNumbers = phoneNumbers,
            Location = ParseLocation(locationLine),
            Profiles = profiles
        };
    }

    private static List<ResumeWorkExperienceEntry> ParseWorkExperience(IReadOnlyList<string>? rawLines)
    {
        var blocks = BuildBlocks(rawLines, splitOnHeaderAfterBullets: true);
        var entries = new List<ResumeWorkExperienceEntry>();

        foreach (var block in blocks)
        {
            var parsed = ParseWorkExperienceBlock(block);
            if (parsed is not null)
            {
                entries.Add(parsed);
            }
        }

        return entries;
    }

    private static ResumeWorkExperienceEntry? ParseWorkExperienceBlock(IReadOnlyList<string> block)
    {
        if (block.Count == 0)
        {
            return null;
        }

        var nonBulletLines = block.Where(line => !IsBulletLine(line)).ToList();
        var descriptionLines = block.Where(IsBulletLine).Select(TrimBulletPrefix).ToList();
        var labeledSkillLines = block
            .Where(line => IsLabeledListLine(line, "skills") || IsLabeledListLine(line, "technologies") || IsLabeledListLine(line, "tools"))
            .ToList();

        descriptionLines.AddRange(nonBulletLines.Where(line => line.Contains("responsible", StringComparison.OrdinalIgnoreCase) ||
                                                                line.Contains("built", StringComparison.OrdinalIgnoreCase) ||
                                                                line.Contains("led", StringComparison.OrdinalIgnoreCase) ||
                                                                line.Contains("delivered", StringComparison.OrdinalIgnoreCase))
                                                .Except(nonBulletLines.Take(2))
                                                .Select(CollapseWhitespace));

        var headerLines = nonBulletLines
            .Where(line => !labeledSkillLines.Contains(line, StringComparer.OrdinalIgnoreCase))
            .Take(3)
            .ToList();

        var dateRange = ExtractDateRange(string.Join(" | ", block));
        var primaryLine = headerLines.FirstOrDefault();
        var secondaryLine = headerLines.Skip(1).FirstOrDefault(line => !ContainsDateRange(line));
        var (jobTitle, employerName) = ParseRoleAndEmployer(primaryLine, secondaryLine);

        if (jobTitle is null && employerName is null && descriptionLines.Count == 0)
        {
            return null;
        }

        var locationLine = headerLines.FirstOrDefault(MightBeLocation);
        var skills = ExtractLabeledItems(block, "skills");
        var technologies = ExtractLabeledItems(block, "technologies", "tools", "tech stack");
        var tags = technologies.Concat(skills)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ResumeWorkExperienceEntry
        {
            EmployerName = employerName,
            EmployerLocation = ParseLocation(locationLine),
            JobTitle = jobTitle,
            EmploymentDates = dateRange,
            DescriptionLines = DistinctNonEmpty(descriptionLines),
            DescriptionMarkdown = ToMarkdownList(descriptionLines),
            Skills = skills,
            Technologies = technologies,
            Tags = tags,
            RawText = string.Join(Environment.NewLine, block)
        };
    }

    private static List<ResumeEducationEntry> ParseEducation(IReadOnlyList<string>? rawLines)
    {
        return BuildBlocks(rawLines)
            .Select(block =>
            {
                var dateRange = ExtractDateRange(string.Join(" | ", block));
                var headerLines = block.Where(line => !IsBulletLine(line)).ToList();
                var institution = headerLines.FirstOrDefault();
                var nextLine = headerLines.Skip(1).FirstOrDefault();
                var descriptionLines = block.Where(IsBulletLine).Select(TrimBulletPrefix).ToList();
                var degree = headerLines.FirstOrDefault(line =>
                    line.Contains("Bachelor", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Master", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("B.S.", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("B.A.", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("M.S.", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Associate", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Certificate", StringComparison.OrdinalIgnoreCase)) ?? nextLine;

                return new ResumeEducationEntry
                {
                    InstitutionName = institution,
                    InstitutionLocation = ParseLocation(headerLines.FirstOrDefault(MightBeLocation)),
                    Degree = degree,
                    FieldOfStudy = ExtractFieldOfStudy(headerLines),
                    AttendanceDates = dateRange,
                    DescriptionLines = DistinctNonEmpty(descriptionLines),
                    RawText = string.Join(Environment.NewLine, block)
                };
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.InstitutionName) || !string.IsNullOrWhiteSpace(entry.Degree))
            .ToList();
    }

    private static List<ResumeSkillSection> ParseSkillSections(IReadOnlyList<string>? rawLines)
    {
        var lines = CleanSectionLines(rawLines);
        if (lines.Count == 0)
        {
            return [];
        }

        var explicitSections = lines
            .Select(line =>
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex <= 0)
                {
                    return null;
                }

                return new ResumeSkillSection
                {
                    Name = CollapseWhitespace(line[..colonIndex]),
                    Items = SplitDelimitedItems(line[(colonIndex + 1)..])
                };
            })
            .Where(static section => section is not null && section.Items.Count > 0)
            .Cast<ResumeSkillSection>()
            .ToList();

        if (explicitSections.Count > 0)
        {
            return explicitSections;
        }

        return
        [
            new ResumeSkillSection
            {
                Name = "Skills",
                Items = lines.SelectMany(SplitDelimitedItems).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            }
        ];
    }

    private static List<ResumeCertification> ParseCertifications(IReadOnlyList<string>? rawLines)
    {
        return BuildBlocks(rawLines)
            .Select(block =>
            {
                var firstLine = block.FirstOrDefault();
                var delimiterIndex = firstLine?.IndexOf(" - ", StringComparison.Ordinal) ?? -1;
                return new ResumeCertification
                {
                    Name = delimiterIndex > 0 ? firstLine![..delimiterIndex] : firstLine,
                    Issuer = delimiterIndex > 0 ? firstLine![(delimiterIndex + 3)..] : block.Skip(1).FirstOrDefault(),
                    CredentialId = block.FirstOrDefault(line => line.Contains("credential", StringComparison.OrdinalIgnoreCase) ||
                                                                line.Contains("license", StringComparison.OrdinalIgnoreCase)),
                    Dates = ExtractDateRange(string.Join(" | ", block)),
                    Url = block.Select(ExtractUrl).FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value)),
                    RawText = string.Join(Environment.NewLine, block)
                };
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
            .ToList();
    }

    private static List<ResumeProjectEntry> ParseProjects(IReadOnlyList<string>? rawLines)
    {
        return BuildBlocks(rawLines)
            .Select(block =>
            {
                var descriptionLines = block.Where(IsBulletLine).Select(TrimBulletPrefix).ToList();
                return new ResumeProjectEntry
                {
                    Title = block.FirstOrDefault(),
                    Dates = ExtractDateRange(string.Join(" | ", block)),
                    DescriptionLines = DistinctNonEmpty(descriptionLines),
                    DescriptionMarkdown = ToMarkdownList(descriptionLines),
                    Skills = ExtractLabeledItems(block, "skills"),
                    Technologies = ExtractLabeledItems(block, "technologies", "tools", "stack"),
                    Url = block.Select(ExtractUrl).FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value)),
                    RepositoryUrl = block.Select(ExtractRepositoryUrl).FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value)),
                    RawText = string.Join(Environment.NewLine, block)
                };
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Title))
            .ToList();
    }

    private static List<ResumeLanguage> ParseLanguages(IReadOnlyList<string>? rawLines)
    {
        var items = CleanSectionLines(rawLines).SelectMany(SplitDelimitedItems).ToList();
        return items.Select(item =>
        {
            var parts = item.Split('-', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 1)
            {
                parts = item.Split(':', 2, StringSplitOptions.TrimEntries);
            }

            return new ResumeLanguage
            {
                Name = parts[0],
                Proficiency = parts.Length > 1 ? parts[1] : null
            };
        }).ToList();
    }

    private static List<ResumeAward> ParseAwards(IReadOnlyList<string>? rawLines)
    {
        return BuildBlocks(rawLines)
            .Select(block =>
            {
                var firstLine = block.FirstOrDefault();
                var dateText = ExtractFirstDateText(block);
                return new ResumeAward
                {
                    Title = firstLine,
                    Issuer = block.FirstOrDefault(line => line != firstLine && !ContainsDateRange(line)),
                    DateText = dateText,
                    Date = TryParseSingleDate(dateText),
                    Description = block.Skip(1).FirstOrDefault()
                };
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Title))
            .ToList();
    }

    private static List<ResumeVolunteerExperienceEntry> ParseVolunteerExperience(IReadOnlyList<string>? rawLines)
    {
        return BuildBlocks(rawLines)
            .Select(block =>
            {
                var firstLine = block.FirstOrDefault();
                var secondLine = block.Skip(1).FirstOrDefault(line => !ContainsDateRange(line));
                var (roleTitle, organizationName) = ParseRoleAndEmployer(firstLine, secondLine);
                var descriptions = block.Where(IsBulletLine).Select(TrimBulletPrefix).ToList();

                return new ResumeVolunteerExperienceEntry
                {
                    OrganizationName = organizationName ?? secondLine,
                    RoleTitle = roleTitle ?? firstLine,
                    Dates = ExtractDateRange(string.Join(" | ", block)),
                    DescriptionLines = DistinctNonEmpty(descriptions),
                    RawText = string.Join(Environment.NewLine, block)
                };
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.OrganizationName) || !string.IsNullOrWhiteSpace(entry.RoleTitle))
            .ToList();
    }

    private static List<ResumePublication> ParsePublications(IReadOnlyList<string>? rawLines)
    {
        return BuildBlocks(rawLines)
            .Select(block =>
            {
                var dateText = ExtractFirstDateText(block);
                return new ResumePublication
                {
                    Title = block.FirstOrDefault(),
                    Publisher = block.Skip(1).FirstOrDefault(line => !ContainsDateRange(line)),
                    DateText = dateText,
                    Date = TryParseSingleDate(dateText),
                    Url = block.Select(ExtractUrl).FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value)),
                    Description = block.Skip(1).FirstOrDefault(line => !line.Contains("http", StringComparison.OrdinalIgnoreCase))
                };
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Title))
            .ToList();
    }

    private static List<ResumeReference> ParseReferences(IReadOnlyList<string>? rawLines)
    {
        return BuildBlocks(rawLines)
            .Select(block =>
            {
                var joined = string.Join(" | ", block);
                return new ResumeReference
                {
                    Name = block.FirstOrDefault(),
                    Relationship = block.FirstOrDefault(line => line.Contains("manager", StringComparison.OrdinalIgnoreCase) ||
                                                                line.Contains("director", StringComparison.OrdinalIgnoreCase) ||
                                                                line.Contains("colleague", StringComparison.OrdinalIgnoreCase) ||
                                                                line.Contains("supervisor", StringComparison.OrdinalIgnoreCase)),
                    Organization = block.Skip(1).FirstOrDefault(line => !line.Contains('@') && !ContainsPhoneNumber(line)),
                    EmailAddress = ExtractEmailAddress(joined),
                    PhoneNumber = ExtractPhoneNumbers(joined).FirstOrDefault(),
                    Note = block.LastOrDefault(line => line != block.FirstOrDefault())
                };
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Name) ||
                            !string.IsNullOrWhiteSpace(entry.EmailAddress) ||
                            !string.IsNullOrWhiteSpace(entry.PhoneNumber))
            .ToList();
    }

    private static List<ResumeCustomSection> ParseAdditionalSections(IReadOnlyDictionary<string, List<string>> sections)
    {
        return sections
            .Where(pair => !pair.Key.Equals("__header__", StringComparison.OrdinalIgnoreCase) &&
                           !HeaderSectionOrder.Contains(pair.Key, StringComparer.OrdinalIgnoreCase))
            .Select(pair => new ResumeCustomSection
            {
                Title = pair.Value.FirstOrDefault(),
                Items = CleanSectionLines(pair.Value),
                RawText = string.Join(Environment.NewLine, pair.Value)
            })
            .Where(section => !string.IsNullOrWhiteSpace(section.Title) || section.Items.Count > 0)
            .ToList();
    }

    private static string? JoinSectionParagraph(IReadOnlyList<string>? rawLines)
    {
        var lines = CleanSectionLines(rawLines);
        return lines.Count == 0 ? null : string.Join(" ", lines);
    }

    private static List<List<string>> BuildBlocks(
        IReadOnlyList<string>? rawLines,
        bool splitOnHeaderAfterBullets = false)
    {
        var lines = CleanSectionLines(rawLines);
        var blocks = new List<List<string>>();
        var current = new List<string>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (current.Count > 0)
                {
                    blocks.Add(current);
                    current = [];
                }

                continue;
            }

            if (current.Count > 0 && IsLikelyEntryBoundary(line, current, splitOnHeaderAfterBullets))
            {
                blocks.Add(current);
                current = [];
            }

            current.Add(CollapseWhitespace(line));
        }

        if (current.Count > 0)
        {
            blocks.Add(current);
        }

        return blocks;
    }

    private static bool IsLikelyEntryBoundary(
        string line,
        IReadOnlyList<string> currentBlock,
        bool splitOnHeaderAfterBullets)
    {
        if (ContainsDateRange(line) && currentBlock.Any(IsBulletLine))
        {
            return true;
        }

        return splitOnHeaderAfterBullets &&
               currentBlock.Any(IsBulletLine) &&
               !IsBulletLine(line) &&
               !ContainsDateRange(line) &&
               !MightBeLocation(line) &&
               !line.Contains(':') &&
               !line.StartsWith("http", StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> CleanSectionLines(IReadOnlyList<string>? rawLines)
    {
        return rawLines?.Select(CollapseWhitespace).ToList() ?? [];
    }

    private static (string? jobTitle, string? employerName) ParseRoleAndEmployer(string? primaryLine, string? secondaryLine)
    {
        primaryLine = CollapseWhitespace(primaryLine);
        secondaryLine = CollapseWhitespace(secondaryLine);

        if (!string.IsNullOrWhiteSpace(primaryLine) && primaryLine.Contains(" at ", StringComparison.OrdinalIgnoreCase))
        {
            var parts = primaryLine.Split(" at ", 2, StringSplitOptions.TrimEntries);
            return (SanitizeHeaderSegment(parts[0]), SanitizeHeaderSegment(parts[1]));
        }

        if (!string.IsNullOrWhiteSpace(primaryLine) &&
            primaryLine.Contains(" | ", StringComparison.Ordinal) &&
            ContainsDateRange(primaryLine))
        {
            primaryLine = DateRangeRegex().Replace(primaryLine, string.Empty).Trim(' ', '|', '-', ',', ';');
        }

        if (!string.IsNullOrWhiteSpace(primaryLine) &&
            primaryLine.Contains(',', StringComparison.Ordinal) &&
            secondaryLine is null)
        {
            var parts = primaryLine.Split(',', 2, StringSplitOptions.TrimEntries);
            return LooksLikeEmployer(parts[0])
                ? (SanitizeHeaderSegment(parts[1]), SanitizeHeaderSegment(parts[0]))
                : (SanitizeHeaderSegment(parts[0]), SanitizeHeaderSegment(parts[1]));
        }

        if (LooksLikeEmployer(primaryLine) && !string.IsNullOrWhiteSpace(secondaryLine))
        {
            return (SanitizeHeaderSegment(secondaryLine), SanitizeHeaderSegment(primaryLine));
        }

        if (LooksLikeEmployer(secondaryLine))
        {
            return (SanitizeHeaderSegment(primaryLine), SanitizeHeaderSegment(secondaryLine));
        }

        return (SanitizeHeaderSegment(primaryLine), SanitizeHeaderSegment(secondaryLine));
    }

    private static bool LooksLikeEmployer(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("Inc", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("LLC", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("Ltd", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("Corp", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("Company", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("University", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("School", StringComparison.OrdinalIgnoreCase);
    }

    private static ResumeDateRange ExtractDateRange(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ResumeDateRange();
        }

        var match = DateRangeRegex().Match(text);
        if (!match.Success)
        {
            return new ResumeDateRange();
        }

        var startText = match.Groups["start"].Value.Trim();
        var endText = match.Groups["end"].Value.Trim();
        var current = endText.Contains("present", StringComparison.OrdinalIgnoreCase) ||
                      endText.Contains("current", StringComparison.OrdinalIgnoreCase) ||
                      endText.Contains("now", StringComparison.OrdinalIgnoreCase);

        return new ResumeDateRange
        {
            StartDateText = startText,
            EndDateText = endText,
            StartDate = TryParsePartialDate(startText),
            EndDate = current ? null : TryParsePartialDate(endText),
            IsCurrent = current
        };
    }

    private static string? ExtractFieldOfStudy(IEnumerable<string> headerLines)
    {
        return headerLines.FirstOrDefault(line =>
            line.Contains(" in ", StringComparison.OrdinalIgnoreCase) ||
            line.Contains(" of ", StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> ExtractLabeledItems(IEnumerable<string> lines, params string[] labels)
    {
        return lines
            .Where(line => labels.Any(label => IsLabeledListLine(line, label)))
            .SelectMany(line =>
            {
                var colonIndex = line.IndexOf(':');
                var value = colonIndex >= 0 ? line[(colonIndex + 1)..] : line;
                return SplitDelimitedItems(value);
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsLabeledListLine(string line, string label)
    {
        return line.StartsWith(label + ":", StringComparison.OrdinalIgnoreCase) ||
               line.StartsWith(label + " -", StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> SplitDelimitedItems(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text
            .Split([",", ";", "|", "\u2022"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(CollapseWhitespace)
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? ExtractFirstDateText(IEnumerable<string> lines)
    {
        return lines.Select(line => DateRangeRegex().Match(line))
            .FirstOrDefault(match => match.Success)?
            .Value;
    }

    private static DateOnly? TryParseSingleDate(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : TryParsePartialDate(value);
    }

    private static DateOnly? TryParsePartialDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = CollapseWhitespace(value)
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace(",", string.Empty, StringComparison.Ordinal);

        string[] formats =
        [
            "MMMM yyyy",
            "MMM yyyy",
            "M/yyyy",
            "MM/yyyy",
            "yyyy-MM",
            "yyyy"
        ];

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(trimmed, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return new DateOnly(parsed.Year, parsed.Month, 1);
            }
        }

        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fallback))
        {
            return new DateOnly(fallback.Year, fallback.Month, 1);
        }

        return null;
    }

    private static bool MightBeLocation(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        return line.Contains(',', StringComparison.Ordinal) &&
               !line.Contains('@') &&
               !ContainsPhoneNumber(line) &&
               !line.StartsWith("http", StringComparison.OrdinalIgnoreCase);
    }

    private static ResumeLocation? ParseLocation(string? line)
    {
        if (!MightBeLocation(line))
        {
            return null;
        }

        var parts = line!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return new ResumeLocation
        {
            City = parts.ElementAtOrDefault(0),
            Region = parts.ElementAtOrDefault(1),
            Country = parts.ElementAtOrDefault(2),
            DisplayText = line
        };
    }

    private static bool LooksLikeContactLine(string line)
    {
        return line.Contains('@') ||
               line.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
               ContainsPhoneNumber(line) ||
               line.Contains("linkedin", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("github", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractEmailAddress(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = EmailRegex().Match(text);
        return match.Success ? match.Value : null;
    }

    private static IEnumerable<string> ExtractPhoneNumbers(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return PhoneRegex().Matches(text).Select(static match => match.Value.Trim());
    }

    private static bool ContainsPhoneNumber(string? text)
    {
        return !string.IsNullOrWhiteSpace(text) && PhoneRegex().IsMatch(text);
    }

    private static List<ResumeProfileLink> ExtractProfileLinks(string line)
    {
        return UrlRegex().Matches(line)
            .Select(match => match.Value)
            .Select(link => new ResumeProfileLink
            {
                Label = InferLinkLabel(link),
                Url = link
            })
            .ToList();
    }

    private static string? ExtractUrl(string line)
    {
        var match = UrlRegex().Match(line);
        return match.Success ? match.Value : null;
    }

    private static string? ExtractRepositoryUrl(string line)
    {
        var url = ExtractUrl(line);
        return url is not null && (url.Contains("github.com", StringComparison.OrdinalIgnoreCase) ||
                                   url.Contains("gitlab.com", StringComparison.OrdinalIgnoreCase) ||
                                   url.Contains("bitbucket.org", StringComparison.OrdinalIgnoreCase))
            ? url
            : null;
    }

    private static string InferLinkLabel(string url)
    {
        if (url.Contains("linkedin", StringComparison.OrdinalIgnoreCase))
        {
            return "LinkedIn";
        }

        if (url.Contains("github", StringComparison.OrdinalIgnoreCase))
        {
            return "GitHub";
        }

        return "Profile";
    }

    private static (string? firstName, string? middleName, string? lastName) SplitFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return (null, null, null);
        }

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            1 => (parts[0], null, null),
            2 => (parts[0], null, parts[1]),
            _ => (parts[0], string.Join(' ', parts[1..^1]), parts[^1])
        };
    }

    private static string CollapseWhitespace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return MultipleSpaceRegex().Replace(value.Trim(), " ");
    }

    private static string? SanitizeHeaderSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var sanitized = DateRangeRegex().Replace(value, string.Empty).Trim(' ', '|', '-', ',', ';');
        return string.IsNullOrWhiteSpace(sanitized) ? null : sanitized;
    }

    private static bool IsLikelyCustomHeading(string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > 40 ||
            value.Contains('@') ||
            value.Contains(':') ||
            value.Contains(',') ||
            value.Contains('|') ||
            value.Contains('.') ||
            value.Contains("http", StringComparison.OrdinalIgnoreCase) ||
            ContainsDateRange(value) ||
            IsBulletLine(value))
        {
            return false;
        }

        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 1)
        {
            return value.Any(char.IsLetter) &&
                   char.IsUpper(value[0]) &&
                   value.Equals(value.Trim(), StringComparison.Ordinal);
        }

        if (words.Length > 4)
        {
            return false;
        }

        var lastWord = words[^1];
        return CustomHeadingKeywords.Contains(lastWord) &&
               words.All(word => char.IsUpper(word[0]));
    }

    private static bool IsBulletLine(string line)
    {
        return line.StartsWith("- ", StringComparison.Ordinal) ||
               line.StartsWith("* ", StringComparison.Ordinal) ||
               line.StartsWith("\u2022", StringComparison.Ordinal) ||
               BulletNumberRegex().IsMatch(line);
    }

    private static string TrimBulletPrefix(string line)
    {
        return BulletPrefixRegex().Replace(line, string.Empty).Trim();
    }

    private static List<string> DistinctNonEmpty(IEnumerable<string> lines)
    {
        return lines
            .Select(CollapseWhitespace)
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? ToMarkdownList(IReadOnlyCollection<string> lines)
    {
        return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines.Select(static line => "- " + line));
    }

    private static bool ContainsDateRange(string? text)
    {
        return !string.IsNullOrWhiteSpace(text) && DateRangeRegex().IsMatch(text);
    }

    private static string GetContentTypeFromFileName(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            _ => "application/octet-stream"
        };
    }

    [GeneratedRegex(@"[^\S\r\n]{2,}")]
    private static partial Regex MultipleSpaceRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex MultipleBlankLineRegex();

    [GeneratedRegex(@"(?<start>(?:jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|sep(?:t(?:ember)?)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?|\d{1,2}/)?\s?\d{4})\s*(?:-|\u2013|to)\s*(?<end>(?:present|current|now|(?:jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|sep(?:t(?:ember)?)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?|\d{1,2}/)?\s?\d{4}))", RegexOptions.IgnoreCase)]
    private static partial Regex DateRangeRegex();

    [GeneratedRegex(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(?:\+?\d{1,2}\s*)?(?:\(?\d{3}\)?[\s.-]*)\d{3}[\s.-]*\d{4}")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"https?://[^\s|]+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"\((?<text>(?:\\.|[^\\)])*)\)\s*Tj", RegexOptions.IgnoreCase)]
    private static partial Regex PdfLiteralTextRegex();

    [GeneratedRegex(@"^\d+\.\s+")]
    private static partial Regex BulletNumberRegex();

    [GeneratedRegex(@"^(?:[-*]\s+|\u2022\s*|\d+\.\s+)")]
    private static partial Regex BulletPrefixRegex();
}

