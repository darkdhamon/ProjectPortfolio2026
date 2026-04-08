using ProjectPortfolio2026.Server.Contracts;

namespace ProjectPortfolio2026.Server.Contracts.Auth;

public sealed class LogoutResponse : ApiResponseDto
{
    public bool SignedOut { get; set; }
}
