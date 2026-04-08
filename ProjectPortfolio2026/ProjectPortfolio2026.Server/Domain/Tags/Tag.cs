using System.ComponentModel.DataAnnotations;

using ProjectPortfolio2026.Server.Domain.Projects;
using ProjectPortfolio2026.Server.Domain.WorkHistory;

namespace ProjectPortfolio2026.Server.Domain.Tags;

public sealed class Tag
{
    public int Id { get; set; }

    public TagCategory Category { get; set; }

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string NormalizedName { get; set; } = string.Empty;

    public List<ProjectTag> ProjectTags { get; set; } = [];

    public List<JobRoleTag> JobRoleTags { get; set; } = [];
}
