using ProjectPortfolio2026.Server.Contracts;

namespace ProjectPortfolio2026.Server.Contracts.Resume;

public sealed class ResumeConfigurationResponse : ApiResponseDto
{
    public int Id { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public string? SourceUrl { get; set; }

    public string? DisplayLabel { get; set; }

    public string? Summary { get; set; }

    public bool IsConfigured { get; set; }
}
