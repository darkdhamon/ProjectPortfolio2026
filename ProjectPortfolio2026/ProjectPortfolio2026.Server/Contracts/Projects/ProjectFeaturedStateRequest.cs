using ProjectPortfolio2026.Server.Contracts;

namespace ProjectPortfolio2026.Server.Contracts.Projects;

public sealed class ProjectFeaturedStateRequest : ApiRequestDto
{
    public bool IsFeatured { get; set; }
}

