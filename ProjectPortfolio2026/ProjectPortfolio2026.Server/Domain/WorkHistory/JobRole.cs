using System.ComponentModel.DataAnnotations;

namespace ProjectPortfolio2026.Server.Domain.WorkHistory;

public sealed class JobRole
{
    public int Id { get; set; }

    public int EmployerId { get; set; }

    public Employer? Employer { get; set; }

    [Required]
    [MaxLength(200)]
    public string Role { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [MaxLength(200)]
    public string? SupervisorName { get; set; }

    [Required]
    public string DescriptionMarkdown { get; set; } = string.Empty;

    public List<JobRoleTag> JobRoleTags { get; set; } = [];
}
