using ProjectPortfolio2026.Server.Contracts.Admin.ResumeImport;
using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Mappers;

public static class ResumeImportContractMapper
{
    public static ResumeImportParseResponse Map(ResumeImportCandidateResult result)
    {
        return new ResumeImportParseResponse
        {
            Person = Map(result.Person),
            CandidateWorkHistory = result.CandidateWorkHistory.Select(Map).ToList(),
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

    private static ResumeImportEmployerCandidateResponse Map(ResumeImportEmployerCandidate candidate)
    {
        return new ResumeImportEmployerCandidateResponse
        {
            CandidateId = candidate.CandidateId,
            EmployerName = candidate.EmployerName,
            EmployerLocation = Map(candidate.EmployerLocation),
            JobRoles = candidate.JobRoles.Select(Map).ToList()
        };
    }

    private static ResumeImportJobRoleCandidateResponse Map(ResumeImportJobRoleCandidate candidate)
    {
        return new ResumeImportJobRoleCandidateResponse
        {
            CandidateId = candidate.CandidateId,
            JobTitle = candidate.JobTitle,
            EmploymentType = candidate.EmploymentType,
            SupervisorName = candidate.SupervisorName,
            EmploymentDates = new ResumeImportDateRangeResponse
            {
                StartDateText = candidate.EmploymentDates.StartDateText,
                EndDateText = candidate.EmploymentDates.EndDateText,
                StartDate = candidate.EmploymentDates.StartDate,
                EndDate = candidate.EmploymentDates.EndDate,
                IsCurrentRole = candidate.EmploymentDates.IsCurrentRole
            },
            DescriptionLines = [.. candidate.DescriptionLines],
            DescriptionMarkdown = candidate.DescriptionMarkdown,
            Skills = [.. candidate.Skills],
            Technologies = [.. candidate.Technologies],
            Tags = [.. candidate.Tags],
            RawRoleText = candidate.RawRoleText,
            RawFields = new Dictionary<string, string?>(candidate.RawFields, StringComparer.Ordinal)
        };
    }
}
