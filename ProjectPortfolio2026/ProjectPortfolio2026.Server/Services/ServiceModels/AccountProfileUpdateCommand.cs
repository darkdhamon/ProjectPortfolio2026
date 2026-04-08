namespace ProjectPortfolio2026.Server.Services.ServiceModels;

public sealed class AccountProfileUpdateCommand
{
    public string UserName { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? DisplayName { get; init; }
}
