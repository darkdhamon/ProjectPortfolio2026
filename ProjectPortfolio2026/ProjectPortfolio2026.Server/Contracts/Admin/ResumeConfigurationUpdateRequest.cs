using System.ComponentModel.DataAnnotations;

namespace ProjectPortfolio2026.Server.Contracts.Admin;

public sealed class ResumeConfigurationUpdateRequest
{
    [Required]
    [MaxLength(30)]
    public string SourceType { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? SourceUrl { get; set; }

    [MaxLength(120)]
    public string? DisplayLabel { get; set; }

    [MaxLength(280)]
    public string? Summary { get; set; }
}
