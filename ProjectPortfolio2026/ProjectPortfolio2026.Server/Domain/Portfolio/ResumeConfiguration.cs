using System.ComponentModel.DataAnnotations;

namespace ProjectPortfolio2026.Server.Domain.Portfolio;

public sealed class ResumeConfiguration
{
    public int Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string SourceType { get; set; } = ResumeSourceTypes.None;

    [MaxLength(2048)]
    public string? SourceUrl { get; set; }

    [MaxLength(120)]
    public string? DisplayLabel { get; set; }

    [MaxLength(280)]
    public string? Summary { get; set; }
}
