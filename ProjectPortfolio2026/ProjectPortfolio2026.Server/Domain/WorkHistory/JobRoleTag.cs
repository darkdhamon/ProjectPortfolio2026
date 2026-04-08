using ProjectPortfolio2026.Server.Domain.Tags;

namespace ProjectPortfolio2026.Server.Domain.WorkHistory;

public sealed class JobRoleTag
{
    public int JobRoleId { get; set; }

    public JobRole? JobRole { get; set; }

    public int TagId { get; set; }

    public Tag? Tag { get; set; }
}
