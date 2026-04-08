using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectPortfolio2026.Server.Contracts.Auth;
using ProjectPortfolio2026.Server.Services.Interfaces;
using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<AuthStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthStatusResponse>> LoginAsync(
        [FromBody] AuthLoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(
            new AuthLoginCommand
            {
                Login = request.Login,
                Password = request.Password
            },
            cancellationToken);
        return result.Succeeded && result.User is not null
            ? Ok(result.User)
            : Unauthorized("Invalid username/email or password.");
    }

    [HttpPost("logout")]
    [ProducesResponseType<LogoutResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LogoutResponse>> LogoutAsync()
    {
        await authService.SignOutAsync();
        return Ok(new LogoutResponse { SignedOut = true });
    }

    [AllowAnonymous]
    [HttpGet("me")]
    [ProducesResponseType<AuthStatusResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthStatusResponse>> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var response = await authService.GetCurrentUserAsync(User, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [HttpPut("me")]
    [ProducesResponseType<AuthStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthStatusResponse>> UpdateCurrentUserAsync(
        [FromBody] AccountProfileUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.UpdateCurrentUserAsync(
            User,
            new AccountProfileUpdateCommand
            {
                UserName = request.UserName,
                Email = request.Email,
                DisplayName = request.DisplayName
            },
            cancellationToken);
        if (result.Succeeded && result.User is not null)
        {
            return Ok(result.User);
        }

        if (result.UserNameConflict)
        {
            return Conflict("Username is already in use.");
        }

        if (result.EmailConflict)
        {
            return Conflict("Email is already in use.");
        }

        return Unauthorized("Authentication is required.");
    }

    [Authorize]
    [HttpPost("me/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ChangePasswordAsync(
        [FromBody] AccountPasswordChangeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.ChangePasswordAsync(
            User,
            new AccountPasswordChangeCommand
            {
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword
            },
            cancellationToken);
        if (result.Succeeded)
        {
            return NoContent();
        }

        if (result.ValidationErrors.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(result.ValidationErrors.ToDictionary(pair => pair.Key, pair => pair.Value)));
        }

        return Unauthorized("Authentication is required.");
    }
}
