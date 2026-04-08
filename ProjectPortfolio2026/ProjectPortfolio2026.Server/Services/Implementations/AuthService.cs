using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectPortfolio2026.Server.Contracts.Auth;
using ProjectPortfolio2026.Server.Domain.Identity;
using ProjectPortfolio2026.Server.Services.Interfaces;
using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Services.Implementations;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : IAuthService
{
    public async Task<LoginResult> LoginAsync(AuthLoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await FindByLoginAsync(request.Login, cancellationToken);
        if (user is null)
        {
            return new LoginResult { Succeeded = false };
        }

        var signInResult = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: false);

        if (!signInResult.Succeeded)
        {
            return new LoginResult { Succeeded = false };
        }

        return new LoginResult
        {
            Succeeded = true,
            User = await CreateStatusResponseAsync(user)
        };
    }

    public async Task SignOutAsync()
    {
        await signInManager.SignOutAsync();
    }

    public async Task<AuthStatusResponse> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return new AuthStatusResponse
            {
                IsAuthenticated = false,
                IsAdmin = false
            };
        }

        return await CreateStatusResponseAsync(user);
    }

    public async Task<ProfileUpdateResult> UpdateCurrentUserAsync(
        ClaimsPrincipal principal,
        AccountProfileUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return new ProfileUpdateResult { Succeeded = false };
        }

        var normalizedUserName = userManager.NormalizeName(request.UserName);
        var existingUser = await userManager.Users
            .Where(candidate => candidate.Id != user.Id)
            .SingleOrDefaultAsync(candidate => candidate.NormalizedUserName == normalizedUserName, cancellationToken);
        if (existingUser is not null)
        {
            return new ProfileUpdateResult
            {
                Succeeded = false,
                UserNameConflict = true
            };
        }

        var trimmedEmail = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedEmail))
        {
            var normalizedEmail = userManager.NormalizeEmail(trimmedEmail);
            var existingEmailUser = await userManager.Users
                .Where(candidate => candidate.Id != user.Id)
                .SingleOrDefaultAsync(candidate => candidate.NormalizedEmail == normalizedEmail, cancellationToken);
            if (existingEmailUser is not null)
            {
                return new ProfileUpdateResult
                {
                    Succeeded = false,
                    EmailConflict = true
                };
            }
        }

        user.UserName = request.UserName.Trim();
        user.NormalizedUserName = normalizedUserName;
        user.Email = trimmedEmail;
        user.NormalizedEmail = trimmedEmail is null ? null : userManager.NormalizeEmail(trimmedEmail);
        user.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim();

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return new ProfileUpdateResult { Succeeded = false };
        }

        await signInManager.RefreshSignInAsync(user);

        return new ProfileUpdateResult
        {
            Succeeded = true,
            User = await CreateStatusResponseAsync(user)
        };
    }

    public async Task<PasswordChangeResult> ChangePasswordAsync(
        ClaimsPrincipal principal,
        AccountPasswordChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return new PasswordChangeResult { Succeeded = false };
        }

        IdentityResult result;
        if (await userManager.HasPasswordAsync(user))
        {
            result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        }
        else
        {
            if (!string.IsNullOrEmpty(request.CurrentPassword))
            {
                return new PasswordChangeResult
                {
                    Succeeded = false,
                    ValidationErrors = new Dictionary<string, string[]>
                    {
                        ["CurrentPassword"] = ["Current password is incorrect."]
                    }
                };
            }

            result = await userManager.AddPasswordAsync(user, request.NewPassword);
        }

        if (!result.Succeeded)
        {
            return new PasswordChangeResult
            {
                Succeeded = false,
                ValidationErrors = result.Errors
                    .GroupBy(error => error.Code, error => error.Description)
                    .ToDictionary(group => group.Key, group => group.ToArray())
            };
        }

        await signInManager.RefreshSignInAsync(user);

        return new PasswordChangeResult { Succeeded = true };
    }

    private async Task<ApplicationUser?> FindByLoginAsync(string login, CancellationToken cancellationToken)
    {
        var normalizedLogin = userManager.NormalizeName(login.Trim());
        var normalizedEmail = userManager.NormalizeEmail(login.Trim());

        return await userManager.Users.SingleOrDefaultAsync(
            user => user.NormalizedUserName == normalizedLogin || user.NormalizedEmail == normalizedEmail,
            cancellationToken);
    }

    private async Task<AuthStatusResponse> CreateStatusResponseAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new AuthStatusResponse
        {
            IsAuthenticated = true,
            IsAdmin = roles.Contains(RoleNames.Admin, StringComparer.Ordinal),
            UserId = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName : user.DisplayName
        };
    }
}
