namespace ProjectPortfolio2026.Server.Services.ServiceModels;

public sealed class AccountPasswordChangeCommand
{
    public string CurrentPassword { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
}
