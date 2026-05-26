using ProjectPortfolio2026.Server.Contracts.Admin.ResumeImport;
using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Mappers;

public static class ResumeImportContractMapper
{
    public static ResumeImportParseResponse Map(ResumeImportParseResult result)
    {
        return new ResumeImportParseResponse
        {
            Person = Map(result.Person),
            WorkHistory = result.WorkHistory.Select(Map).ToList(),
            GlobalSkills = [.. result.GlobalSkills],
            ProfessionalSummary = result.ProfessionalSummary,
            RawText = result.RawText,
            SourceFileName = result.SourceFileName,
            ParserName = result.ParserName
        };
    }

    private static ResumeImportPersonResponse? Map(ParsedPerson? person)
    {
        if (person is null)
        {
            return null;
        }

        return new ResumeImportPersonResponse
        {
            FullName = person.FullName,
            FirstName = person.FirstName,
            MiddleName = person.MiddleName,
            LastName = person.LastName,
            Headline = person.Headline,
            EmailAddress = person.EmailAddress,
            PhoneNumbers = [.. person.PhoneNumbers],
            Location = Map(person.Location),
            SocialProfiles = [.. person.SocialProfiles]
        };
    }

    private static ResumeImportLocationResponse? Map(ParsedLocation? location)
    {
        if (location is null)
        {
            return null;
        }

        return new ResumeImportLocationResponse
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

    private static ResumeImportWorkHistoryEntryResponse Map(ParsedWorkHistoryEntry entry)
    {
        return new ResumeImportWorkHistoryEntryResponse
        {
            EmployerName = entry.EmployerName,
            EmployerLocation = Map(entry.EmployerLocation),
            JobTitle = entry.JobTitle,
            EmploymentType = entry.EmploymentType,
            SupervisorName = entry.SupervisorName,
            EmploymentDates = new ResumeImportDateRangeResponse
            {
                StartDateText = entry.EmploymentDates.StartDateText,
                EndDateText = entry.EmploymentDates.EndDateText,
                StartDate = entry.EmploymentDates.StartDate,
                EndDate = entry.EmploymentDates.EndDate,
                IsCurrentRole = entry.EmploymentDates.IsCurrentRole
            },
            DescriptionLines = [.. entry.DescriptionLines],
            DescriptionMarkdown = entry.DescriptionMarkdown,
            Skills = [.. entry.Skills],
            Technologies = [.. entry.Technologies],
            Tags = [.. entry.Tags],
            RawRoleText = entry.RawRoleText,
            RawFields = new Dictionary<string, string?>(entry.RawFields, StringComparer.Ordinal)
        };
    }
}
