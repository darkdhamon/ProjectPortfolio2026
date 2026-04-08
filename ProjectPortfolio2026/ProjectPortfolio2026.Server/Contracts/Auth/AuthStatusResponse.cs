using ProjectPortfolio2026.Server.Contracts;

namespace ProjectPortfolio2026.Server.Contracts.Auth;

public sealed class AuthStatusResponse : ApiResponseDto
{
    public bool IsAuthenticated { get; set; }

    public bool IsAdmin { get; set; }

    public string? UserId { get; set; }

    public string? UserName { get; set; }

    public string? Email { get; set; }

    public string? DisplayName { get; set; }
}
