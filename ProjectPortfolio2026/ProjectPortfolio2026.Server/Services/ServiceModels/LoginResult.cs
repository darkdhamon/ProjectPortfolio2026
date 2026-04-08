using ProjectPortfolio2026.Server.Contracts.Auth;

namespace ProjectPortfolio2026.Server.Services.ServiceModels;

public sealed class LoginResult
{
    public bool Succeeded { get; init; }

    public AuthStatusResponse? User { get; init; }
}
