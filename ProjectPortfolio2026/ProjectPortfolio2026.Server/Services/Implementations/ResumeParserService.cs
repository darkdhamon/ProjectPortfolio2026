using ProjectPortfolio2026.ResumeParser.Interfaces;
using ProjectPortfolio2026.ResumeParser.Models;
using ProjectPortfolio2026.Server.Services.Interfaces;
using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Services.Implementations;

public sealed class ResumeParserService(IResumeDocumentParser resumeDocumentParser) : IResumeParserService
{
    public async Task<ResumeImportParseResult> ParseAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var document = await resumeDocumentParser.ParseAsync(content, fileName, cancellationToken);

        return new ResumeImportParseResult
        {
            Person = MapPerson(document.Header),
            WorkHistory = document.WorkExperience
                .Select(MapWorkHistoryEntry)
                .ToList(),
            GlobalSkills = document.Skills
                .SelectMany(section => section.Items)
                .Where(skill => !string.IsNullOrWhiteSpace(skill))
                .Select(skill => skill.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(skill => skill, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            ProfessionalSummary = document.ProfessionalSummary,
            RawText = document.RawText,
            SourceFileName = document.SourceFileName,
            ParserName = document.ParserName
        };
    }

    private static ParsedPerson? MapPerson(ResumeHeader? header)
    {
        if (header is null)
        {
            return null;
        }

        return new ParsedPerson
        {
            FullName = header.FullName,
            FirstName = header.FirstName,
            MiddleName = header.MiddleName,
            LastName = header.LastName,
            Headline = header.Headline,
            EmailAddress = header.EmailAddress,
            PhoneNumbers = [.. header.PhoneNumbers],
            Location = MapLocation(header.Location),
            SocialProfiles = header.Profiles
                .Select(profile => profile.Url ?? profile.Label ?? profile.UserName)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim())
                .ToList()
        };
    }

    private static ParsedWorkHistoryEntry MapWorkHistoryEntry(ResumeWorkExperienceEntry entry)
    {
        return new ParsedWorkHistoryEntry
        {
            EmployerName = entry.EmployerName,
            EmployerLocation = MapLocation(entry.EmployerLocation),
            JobTitle = entry.JobTitle,
            EmploymentType = entry.EmploymentType,
            SupervisorName = entry.SupervisorName,
            EmploymentDates = new ParsedDateRange
            {
                StartDateText = entry.EmploymentDates.StartDateText,
                EndDateText = entry.EmploymentDates.EndDateText,
                StartDate = entry.EmploymentDates.StartDate,
                EndDate = entry.EmploymentDates.EndDate,
                IsCurrentRole = entry.EmploymentDates.IsCurrent
            },
            DescriptionLines = [.. entry.DescriptionLines],
            DescriptionMarkdown = entry.DescriptionMarkdown,
            Skills = [.. entry.Skills],
            Technologies = [.. entry.Technologies],
            Tags = [.. entry.Tags],
            RawRoleText = entry.RawText,
            RawFields = new Dictionary<string, string?>(entry.Metadata, StringComparer.Ordinal)
        };
    }

    private static ParsedLocation? MapLocation(ResumeLocation? location)
    {
        if (location is null)
        {
            return null;
        }

        return new ParsedLocation
        {
            StreetAddress1 = location.StreetAddress1,
            StreetAddress2 = location.StreetAddress2,
            City = location.City,
            Region = location.Region,
            PostalCode = location.PostalCode,
            Country = location.Country,
            DisplayText = location.DisplayText
        };
    }
}
