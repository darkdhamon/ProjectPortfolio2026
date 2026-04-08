using ProjectPortfolio2026.Server.Domain.Projects;

namespace ProjectPortfolio2026.Server.Services.Interfaces;

public interface IProjectTagNormalizer
{
    Task NormalizeAsync(Project project, CancellationToken cancellationToken = default);
}
