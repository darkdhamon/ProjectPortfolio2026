namespace ProjectPortfolio2026.Server.Contracts.Admin;

public sealed class AdminSocialLinkResponse
{
    public int Id { get; set; }

    public string Platform { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string? Handle { get; set; }

    public string? Summary { get; set; }

    public int SortOrder { get; set; }

    public bool IsVisible { get; set; }
}
