using System.ComponentModel.DataAnnotations;

namespace ProjectPortfolio2026.Server.Domain.Portfolio;

public sealed class PortfolioContactMethod
{
    public int Id { get; set; }

    public int PortfolioProfileId { get; set; }

    public PortfolioProfile PortfolioProfile { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Label { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Value { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Href { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    public int SortOrder { get; set; }

    public bool IsVisible { get; set; }
}
