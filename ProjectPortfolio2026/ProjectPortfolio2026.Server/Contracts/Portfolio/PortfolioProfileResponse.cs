namespace ProjectPortfolio2026.Server.Contracts.Portfolio;

public sealed class PortfolioProfileResponse
{
    public string? RequestId { get; set; }

    public int Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string ContactHeadline { get; set; } = string.Empty;

    public string ContactIntro { get; set; } = string.Empty;

    public string? AvailabilityHeadline { get; set; }

    public string? AvailabilitySummary { get; set; }

    public List<PortfolioContactMethodResponse> ContactMethods { get; set; } = [];

    public List<PortfolioSocialLinkResponse> SocialLinks { get; set; } = [];
}
