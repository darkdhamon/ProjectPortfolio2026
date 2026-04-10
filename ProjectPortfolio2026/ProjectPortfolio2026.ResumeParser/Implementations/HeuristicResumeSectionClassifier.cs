using ProjectPortfolio2026.ResumeParser.Interfaces;

namespace ProjectPortfolio2026.ResumeParser.Implementations;

internal sealed class HeuristicResumeSectionClassifier : IResumeSectionClassifier
{
    private static readonly string[] StandardSectionKeys =
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

    public Dictionary<string, List<string>> Classify(IReadOnlyList<string> lines)
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
                    // Keep resume header lines in the header until a real section boundary appears.
                }
                else
                {
                    currentSection = normalizedSection;
                    if (!sections.ContainsKey(currentSection))
                    {
                        sections[currentSection] = [];
                    }

                    if (!StandardSectionKeys.Contains(currentSection, StringComparer.OrdinalIgnoreCase))
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

            sections[currentSection].Add(line);
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

        var candidate = ResumeParsingUtilities.CollapseWhitespace(originalHeading).ToLowerInvariant();
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
            ResumeParsingUtilities.ContainsDateRange(value) ||
            ResumeParsingUtilities.IsBulletLine(value))
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
}
