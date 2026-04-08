using System.ComponentModel.DataAnnotations;
using ProjectPortfolio2026.Server.Contracts;

namespace ProjectPortfolio2026.Server.Contracts.Auth;

public sealed class AuthLoginRequest : ApiRequestDto
{
    [Required]
    public string Login { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
