using System.Security.Claims;
using ProjectPortfolio2026.Server.Contracts.Auth;
using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(AuthLoginCommand command, CancellationToken cancellationToken = default);

    Task SignOutAsync();

    Task<AuthStatusResponse> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    Task<ProfileUpdateResult> UpdateCurrentUserAsync(
        ClaimsPrincipal principal,
        AccountProfileUpdateCommand command,
        CancellationToken cancellationToken = default);

    Task<PasswordChangeResult> ChangePasswordAsync(
        ClaimsPrincipal principal,
        AccountPasswordChangeCommand command,
        CancellationToken cancellationToken = default);
}
