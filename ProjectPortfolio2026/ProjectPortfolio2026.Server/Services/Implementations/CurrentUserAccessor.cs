using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ProjectPortfolio2026.Server.Domain.Identity;
using ProjectPortfolio2026.Server.Services.Interfaces;

namespace ProjectPortfolio2026.Server.Services.Implementations;

public sealed class CurrentUserAccessor(UserManager<ApplicationUser> userManager) : ICurrentUserAccessor
{
    public Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        return userManager.GetUserAsync(principal);
    }
}
