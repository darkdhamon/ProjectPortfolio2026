using ProjectPortfolio2026.Server.Contracts;

namespace ProjectPortfolio2026.Server.Contracts.Projects;

public sealed class ProjectListResponse : ApiResponseDto
{
    public List<ProjectResponse> Items { get; set; } = [];
}
