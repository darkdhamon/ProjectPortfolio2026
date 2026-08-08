using System.Collections.Generic;

using ProjectPortfolio2026.Server.Contracts;

namespace ProjectPortfolio2026.Server.Contracts.Projects;

public sealed class ProjectFeaturedOrderUpdateRequest : ApiRequestDto
{
    public IReadOnlyCollection<int> ProjectIds { get; set; } = [];
}

