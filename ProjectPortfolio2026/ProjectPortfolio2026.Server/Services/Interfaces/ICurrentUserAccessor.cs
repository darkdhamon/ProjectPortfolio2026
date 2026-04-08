using System.Security.Claims;
using ProjectPortfolio2026.Server.Domain.Identity;

namespace ProjectPortfolio2026.Server.Services.Interfaces;

public interface ICurrentUserAccessor
{
    Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal principal);
}
