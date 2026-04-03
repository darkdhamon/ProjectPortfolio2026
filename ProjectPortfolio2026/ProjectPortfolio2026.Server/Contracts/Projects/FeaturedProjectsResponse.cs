using ProjectPortfolio2026.Server.Contracts;

namespace ProjectPortfolio2026.Server.Contracts.Projects;

public sealed class FeaturedProjectsResponse : ApiResponseDto
{
    public List<ProjectSummaryResponse> Items { get; set; } = [];
}
