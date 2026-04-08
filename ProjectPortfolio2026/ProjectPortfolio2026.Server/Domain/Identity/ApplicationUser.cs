using Microsoft.AspNetCore.Identity;

namespace ProjectPortfolio2026.Server.Domain.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
