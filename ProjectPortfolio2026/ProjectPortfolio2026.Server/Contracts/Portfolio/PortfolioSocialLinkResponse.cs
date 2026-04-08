namespace ProjectPortfolio2026.Server.Contracts.Portfolio;

public sealed class PortfolioSocialLinkResponse
{
    public string Platform { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string? Handle { get; set; }

    public string? Summary { get; set; }

    public int SortOrder { get; set; }
}
