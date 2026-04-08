namespace ProjectPortfolio2026.Server.Contracts.WorkHistory;

public sealed class JobRoleResponse
{
    public string Role { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? SupervisorName { get; set; }

    public string DescriptionMarkdown { get; set; } = string.Empty;

    public List<string> Skills { get; set; } = [];

    public List<string> Technologies { get; set; } = [];
}
