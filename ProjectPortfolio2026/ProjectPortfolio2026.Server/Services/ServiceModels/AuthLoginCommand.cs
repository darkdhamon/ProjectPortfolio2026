namespace ProjectPortfolio2026.Server.Services.ServiceModels;

public sealed class AuthLoginCommand
{
    public string Login { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
