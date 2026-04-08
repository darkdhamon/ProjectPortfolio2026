using ProjectPortfolio2026.Server.Contracts.WorkHistory;
using ProjectPortfolio2026.Server.Domain.Tags;
using ProjectPortfolio2026.Server.Domain.WorkHistory;

namespace ProjectPortfolio2026.Server.Mappers;

public static class WorkHistoryContractMapper
{
    public static EmployerResponse ToResponse(this Employer employer)
    {
        return new EmployerResponse
        {
            Id = employer.Id,
            Name = employer.Name,
            StreetAddress1 = employer.StreetAddress1,
            StreetAddress2 = employer.StreetAddress2,
            City = employer.City,
            Region = employer.Region,
            PostalCode = employer.PostalCode,
            Country = employer.Country,
            JobRoles = employer.JobRoles
                .OrderBy(jobRole => jobRole.EndDate.HasValue ? 1 : 0)
                .ThenByDescending(jobRole => jobRole.EndDate ?? jobRole.StartDate)
                .ThenByDescending(jobRole => jobRole.StartDate)
                .ThenBy(jobRole => jobRole.Role)
                .Select(jobRole => jobRole.ToResponse())
                .ToList()
        };
    }

    public static JobRoleResponse ToResponse(this JobRole jobRole)
    {
        return new JobRoleResponse
        {
            Role = jobRole.Role,
            StartDate = jobRole.StartDate,
            EndDate = jobRole.EndDate,
            SupervisorName = jobRole.SupervisorName,
            DescriptionMarkdown = jobRole.DescriptionMarkdown,
            Skills = jobRole.JobRoleTags
                .Where(jobRoleTag => jobRoleTag.Tag?.Category == TagCategory.Skill)
                .Select(jobRoleTag => jobRoleTag.Tag!.DisplayName)
                .OrderBy(tag => tag)
                .ToList(),
            Technologies = jobRole.JobRoleTags
                .Where(jobRoleTag => jobRoleTag.Tag?.Category == TagCategory.Technology)
                .Select(jobRoleTag => jobRoleTag.Tag!.DisplayName)
                .OrderBy(tag => tag)
                .ToList()
        };
    }
}
