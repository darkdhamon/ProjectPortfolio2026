using ProjectPortfolio2026.Server.Contracts;

namespace ProjectPortfolio2026.Server.Contracts.Projects;

public sealed class ProjectListQueryRequest : ApiRequestDto
{
    public string? Search { get; set; }

    public string? Skills { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 6;
}
