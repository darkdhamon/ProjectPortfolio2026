namespace ProjectPortfolio2026.Server.Contracts.Portfolio;

public sealed class PortfolioContactMethodResponse
{
    public string Type { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string? Href { get; set; }

    public string? Note { get; set; }

    public int SortOrder { get; set; }
}
