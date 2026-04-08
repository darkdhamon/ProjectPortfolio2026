using System.ComponentModel.DataAnnotations;
using ProjectPortfolio2026.Server.Contracts;

namespace ProjectPortfolio2026.Server.Contracts.Auth;

public sealed class AccountProfileUpdateRequest : ApiRequestDto
{
    [Required]
    [StringLength(256)]
    public string UserName { get; set; } = string.Empty;

    [StringLength(256)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(256)]
    public string? DisplayName { get; set; }
}
