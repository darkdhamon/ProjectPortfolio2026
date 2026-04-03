using ProjectPortfolio2026.Server.Contracts;

namespace ProjectPortfolio2026.Server.Contracts.Projects;

public sealed class ProjectListResponse : ApiResponseDto
{
    public List<ProjectSummaryResponse> Items { get; set; } = [];

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public bool HasMore { get; set; }

    public List<string> AvailableSkills { get; set; } = [];
}
