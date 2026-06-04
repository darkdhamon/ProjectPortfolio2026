namespace ProjectPortfolio2026.Server.Services.ServiceModels;

public sealed class ResumeImportEmployerCandidate
{
    public string CandidateId { get; init; } = string.Empty;

    public string? EmployerName { get; init; }

    public ParsedLocation? EmployerLocation { get; init; }

    public List<ResumeImportJobRoleCandidate> JobRoles { get; init; } = [];
}
