using System.ComponentModel.DataAnnotations;

namespace ProjectPortfolio2026.Server.Domain.Portfolio;

public sealed class PortfolioSocialLink
{
    public int Id { get; set; }

    public int PortfolioProfileId { get; set; }

    public PortfolioProfile PortfolioProfile { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Platform { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Label { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Handle { get; set; }

    [MaxLength(500)]
    public string? Summary { get; set; }

    public int SortOrder { get; set; }

    public bool IsVisible { get; set; }
}
