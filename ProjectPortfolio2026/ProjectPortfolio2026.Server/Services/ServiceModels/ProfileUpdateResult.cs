using ProjectPortfolio2026.Server.Contracts.Auth;

namespace ProjectPortfolio2026.Server.Services.ServiceModels;

public sealed class ProfileUpdateResult
{
    public bool Succeeded { get; init; }

    public bool UserNameConflict { get; init; }

    public bool EmailConflict { get; init; }

    public AuthStatusResponse? User { get; init; }
}
