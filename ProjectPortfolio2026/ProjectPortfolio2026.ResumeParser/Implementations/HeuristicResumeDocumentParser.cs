using System.Globalization;
using ProjectPortfolio2026.ResumeParser.Interfaces;
using ProjectPortfolio2026.ResumeParser.Models;

namespace ProjectPortfolio2026.ResumeParser.Implementations;

public sealed class HeuristicResumeDocumentParser(
    IEnumerable<IResumeTextExtractor> textExtractors,
    IResumeSectionClassifier sectionClassifier,
    IEnumerable<IResumeSectionParser> sectionParsers,
    IResumeAdditionalSectionParser additionalSectionParser) : IResumeDocumentParser
{
    public HeuristicResumeDocumentParser()
        : this(
            [
                new DocxResumeTextExtractor(),
                new PdfResumeTextExtractor(),
                new PlainTextResumeTextExtractor()
            ],
            new HeuristicResumeSectionClassifier(),
            [
                new HeaderResumeSectionParser(),
                new ProfessionalSummaryResumeSectionParser(),
                new WorkExperienceResumeSectionParser(),
                new EducationResumeSectionParser(),
                new SkillsResumeSectionParser(),
                new CertificationsResumeSectionParser(),
                new ProjectsResumeSectionParser(),
                new LanguagesResumeSectionParser(),
                new AwardsResumeSectionParser(),
                new VolunteerExperienceResumeSectionParser(),
                new PublicationsResumeSectionParser(),
                new ReferencesResumeSectionParser()
            ],
            new AdditionalResumeSectionParser())
    {
    }

    private readonly IResumeTextExtractor[] textExtractors = textExtractors.ToArray();
    private readonly IResumeSectionParser[] sectionParsers = sectionParsers.ToArray();

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

        var extractor = ResolveExtractor(fileName);
        var rawText = await extractor.ExtractAsync(content, fileName, cancellationToken);
        var normalizedText = ResumeParsingUtilities.NormalizeText(rawText);
        var lines = normalizedText.Split('\n').Select(static line => line.TrimEnd()).ToList();
        var sections = sectionClassifier.Classify(lines);

        var builder = new ResumeDocumentBuilder();
        foreach (var parser in sectionParsers)
        {
            sections.TryGetValue(parser.SectionKey, out var sectionLines);
            parser.Parse(sectionLines, builder);
        }

        builder.AdditionalSections = additionalSectionParser.Parse(sections);

        return builder.Build(
            normalizedText,
            fileName,
            nameof(HeuristicResumeDocumentParser),
            GetContentTypeFromFileName(fileName));
    }

    private IResumeTextExtractor ResolveExtractor(string fileName)
    {
        return textExtractors.FirstOrDefault(extractor => extractor.CanExtract(fileName))
            ?? throw new NotSupportedException(
                $"The resume parser does not support '{Path.GetExtension(fileName)}' files yet. Supported formats currently include .docx, .pdf, .txt, and .md.");
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
}

public sealed class ResumeDocumentBuilder
{
    public ResumeHeader Header { get; set; } = new();

    public string? ProfessionalSummary { get; set; }

    public List<ResumeWorkExperienceEntry> WorkExperience { get; set; } = [];

    public List<ResumeEducationEntry> Education { get; set; } = [];

    public List<ResumeSkillSection> Skills { get; set; } = [];

    public List<ResumeCertification> Certifications { get; set; } = [];

    public List<ResumeProjectEntry> Projects { get; set; } = [];

    public List<ResumeLanguage> Languages { get; set; } = [];

    public List<ResumeAward> Awards { get; set; } = [];

    public List<ResumeVolunteerExperienceEntry> VolunteerExperience { get; set; } = [];

    public List<ResumePublication> Publications { get; set; } = [];

    public List<ResumeReference> References { get; set; } = [];

    public List<ResumeCustomSection> AdditionalSections { get; set; } = [];

    public ResumeDocument Build(string rawText, string fileName, string parserName, string contentType)
    {
        return new ResumeDocument
        {
            Header = Header,
            ProfessionalSummary = ProfessionalSummary,
            WorkExperience = WorkExperience,
            Education = Education,
            Skills = Skills,
            Certifications = Certifications,
            Projects = Projects,
            Languages = Languages,
            Awards = Awards,
            VolunteerExperience = VolunteerExperience,
            Publications = Publications,
            References = References,
            AdditionalSections = AdditionalSections,
            RawText = rawText,
            SourceFileName = fileName,
            ParserName = parserName,
            Metadata = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["contentType"] = contentType,
                ["parsedAtUtc"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            }
        };
    }
}
