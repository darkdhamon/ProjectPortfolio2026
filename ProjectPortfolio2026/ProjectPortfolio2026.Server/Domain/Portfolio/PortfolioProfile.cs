using System.ComponentModel.DataAnnotations;

namespace ProjectPortfolio2026.Server.Domain.Portfolio;

public sealed class PortfolioProfile
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string ContactHeadline { get; set; } = string.Empty;

    [Required]
    public string ContactIntro { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? AvailabilityHeadline { get; set; }

    [MaxLength(500)]
    public string? AvailabilitySummary { get; set; }

    public bool IsPublic { get; set; }

    public List<PortfolioContactMethod> ContactMethods { get; set; } = [];

    public List<PortfolioSocialLink> SocialLinks { get; set; } = [];
}
