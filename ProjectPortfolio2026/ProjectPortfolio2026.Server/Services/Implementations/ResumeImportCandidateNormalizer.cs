using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Services.Implementations;

public static class ResumeImportCandidateNormalizer
{
    public static ResumeImportCandidateResult Normalize(ResumeImportParseResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new ResumeImportCandidateResult
        {
            Person = NormalizePerson(result.Person),
            CandidateWorkHistory = NormalizeWorkHistory(result.WorkHistory),
            GlobalSkills = NormalizeValues(result.GlobalSkills),
            ProfessionalSummary = NormalizeText(result.ProfessionalSummary),
            RawText = NormalizeText(result.RawText),
            SourceFileName = NormalizeText(result.SourceFileName),
            ParserName = NormalizeText(result.ParserName)
        };
    }

    private static ParsedPerson? NormalizePerson(ParsedPerson? person)
    {
        if (person is null)
        {
            return null;
        }

        return new ParsedPerson
        {
            FullName = NormalizeText(person.FullName),
            FirstName = NormalizeText(person.FirstName),
            MiddleName = NormalizeText(person.MiddleName),
            LastName = NormalizeText(person.LastName),
            Headline = NormalizeText(person.Headline),
            EmailAddress = NormalizeText(person.EmailAddress),
            PhoneNumbers = NormalizeValues(person.PhoneNumbers),
            Location = NormalizeLocation(person.Location),
            SocialProfiles = NormalizeValues(person.SocialProfiles)
        };
    }

    private static List<ResumeImportEmployerCandidate> NormalizeWorkHistory(IEnumerable<ParsedWorkHistoryEntry> workHistory)
    {
        var candidates = new List<ResumeImportEmployerCandidate>();
        var employerLookup = new Dictionary<string, int>(StringComparer.Ordinal);
        var employerCount = 0;
        var roleCount = 0;

        foreach (var entry in workHistory)
        {
            var employerName = NormalizeText(entry.EmployerName);
            var employerLocation = NormalizeLocation(entry.EmployerLocation);
            var employerKey = CreateEmployerKey(employerName, employerLocation, employerCount);

            if (!employerLookup.TryGetValue(employerKey, out var index))
            {
                employerCount++;
                index = candidates.Count;
                employerLookup[employerKey] = index;
                candidates.Add(new ResumeImportEmployerCandidate
                {
                    CandidateId = $"employer-{employerCount:D3}",
                    EmployerName = employerName,
                    EmployerLocation = employerLocation,
                    JobRoles = []
                });
            }

            roleCount++;
            candidates[index].JobRoles.Add(new ResumeImportJobRoleCandidate
            {
                CandidateId = $"role-{roleCount:D3}",
                JobTitle = NormalizeText(entry.JobTitle),
                EmploymentDates = NormalizeDateRange(entry.EmploymentDates),
                EmploymentType = NormalizeText(entry.EmploymentType),
                SupervisorName = NormalizeText(entry.SupervisorName),
                DescriptionLines = NormalizeValues(entry.DescriptionLines),
                DescriptionMarkdown = NormalizeDescriptionMarkdown(entry.DescriptionMarkdown, entry.DescriptionLines),
                Skills = NormalizeValues(entry.Skills),
                Technologies = NormalizeValues(entry.Technologies),
                Tags = NormalizeValues(entry.Tags),
                RawRoleText = NormalizeText(entry.RawRoleText),
                RawFields = NormalizeRawFields(entry.RawFields)
            });
        }

        return candidates;
    }

    private static string CreateEmployerKey(string? employerName, ParsedLocation? employerLocation, int fallbackIndex)
    {
        var locationKey = NormalizeLocationKey(employerLocation);

        if (!string.IsNullOrEmpty(employerName))
        {
            return $"{employerName.ToUpperInvariant()}|{locationKey}";
        }

        return $"UNNAMED|{locationKey}|{fallbackIndex:D3}";
    }

    private static string NormalizeLocationKey(ParsedLocation? location)
    {
        if (location is null)
        {
            return string.Empty;
        }

        var locationParts = new[]
        {
            location.StreetAddress1,
            location.StreetAddress2,
            location.City,
            location.Region,
            location.PostalCode,
            location.Country,
            location.DisplayText
        };

        return string.Join("|", locationParts.Select(part => NormalizeText(part)?.ToUpperInvariant() ?? string.Empty));
    }

    private static ParsedDateRange NormalizeDateRange(ParsedDateRange? dates)
    {
        if (dates is null)
        {
            return new ParsedDateRange();
        }

        return new ParsedDateRange
        {
            StartDateText = NormalizeText(dates.StartDateText),
            EndDateText = NormalizeText(dates.EndDateText),
            StartDate = dates.StartDate,
            EndDate = dates.EndDate,
            IsCurrentRole = dates.IsCurrentRole
        };
    }

    private static ParsedLocation? NormalizeLocation(ParsedLocation? location)
    {
        if (location is null)
        {
            return null;
        }

        var normalized = new ParsedLocation
        {
            StreetAddress1 = NormalizeText(location.StreetAddress1),
            StreetAddress2 = NormalizeText(location.StreetAddress2),
            City = NormalizeText(location.City),
            Region = NormalizeText(location.Region),
            PostalCode = NormalizeText(location.PostalCode),
            Country = NormalizeText(location.Country),
            DisplayText = NormalizeText(location.DisplayText)
        };

        return normalized.StreetAddress1 is null
            && normalized.StreetAddress2 is null
            && normalized.City is null
            && normalized.Region is null
            && normalized.PostalCode is null
            && normalized.Country is null
            && normalized.DisplayText is null
                ? null
                : normalized;
    }

    private static string? NormalizeDescriptionMarkdown(string? descriptionMarkdown, IEnumerable<string> descriptionLines)
    {
        var normalizedMarkdown = NormalizeText(descriptionMarkdown);
        if (normalizedMarkdown is not null)
        {
            return normalizedMarkdown;
        }

        var normalizedLines = NormalizeValues(descriptionLines);
        return normalizedLines.Count == 0
            ? null
            : string.Join(Environment.NewLine, normalizedLines);
    }

    private static Dictionary<string, string?> NormalizeRawFields(Dictionary<string, string?> rawFields)
    {
        return rawFields
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(
                pair => pair.Key.Trim(),
                pair => NormalizeText(pair.Value),
                StringComparer.Ordinal);
    }

    private static List<string> NormalizeValues(IEnumerable<string?> values)
    {
        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values)
        {
            var trimmed = NormalizeText(value);
            if (trimmed is null || !seen.Add(trimmed))
            {
                continue;
            }

            normalized.Add(trimmed);
        }

        return normalized;
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
