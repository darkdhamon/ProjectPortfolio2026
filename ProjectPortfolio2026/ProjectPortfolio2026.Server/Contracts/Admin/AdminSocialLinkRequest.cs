namespace ProjectPortfolio2026.Server.Contracts.Admin;

public sealed class AdminSocialLinksUpdateRequest
{
    public List<AdminSocialLinkRequest> SocialLinks { get; set; } = [];
}

public sealed class AdminSocialLinkRequest
{
    public int? Id { get; set; }

    public string? Platform { get; set; }

    public string? Label { get; set; }

    public string? Url { get; set; }

    public string? Handle { get; set; }

    public string? Summary { get; set; }

    public int? SortOrder { get; set; }

    public bool? IsVisible { get; set; }
}
