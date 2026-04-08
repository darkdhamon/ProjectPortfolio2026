using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Contracts.Auth;
using ProjectPortfolio2026.Server.Controllers;
using ProjectPortfolio2026.Server.Services.Interfaces;
using ProjectPortfolio2026.Server.Services.ServiceModels;
using System.Security.Claims;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class AuthControllerTests
{
    [Test]
    public async Task LoginAsync_ReturnsUnauthorized_WhenCredentialsAreInvalid()
    {
        var authService = new StubAuthService
        {
            LoginResult = new LoginResult { Succeeded = false }
        };
        var controller = CreateController(authService);

        var result = await controller.LoginAsync(
            new AuthLoginRequest
            {
                Login = "admin",
                Password = "invalid"
            },
            CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetCurrentUserAsync_ReturnsCurrentUserState()
    {
        var authService = new StubAuthService
        {
            CurrentUserResponse = new AuthStatusResponse
            {
                IsAuthenticated = true,
                IsAdmin = true,
                UserName = "admin"
            }
        };
        var controller = CreateController(authService);

        var result = await controller.GetCurrentUserAsync(CancellationToken.None);
        var okResult = result.Result as OkObjectResult;
        var response = okResult?.Value as AuthStatusResponse;

        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.IsAuthenticated, Is.True);
            Assert.That(response.IsAdmin, Is.True);
            Assert.That(response.UserName, Is.EqualTo("admin"));
        });
    }

    [Test]
    public async Task UpdateCurrentUserAsync_ReturnsConflict_WhenUsernameIsTaken()
    {
        var authService = new StubAuthService
        {
            UpdateResult = new ProfileUpdateResult
            {
                Succeeded = false,
                UserNameConflict = true
            }
        };
        var controller = CreateController(authService);

        var result = await controller.UpdateCurrentUserAsync(
            new AccountProfileUpdateRequest
            {
                UserName = "existing-user"
            },
            CancellationToken.None);

        var conflict = result.Result as ConflictObjectResult;
        Assert.That(conflict?.Value, Is.EqualTo("Username is already in use."));
    }

    [Test]
    public async Task ChangePasswordAsync_ReturnsValidationProblem_WhenPasswordChangeFailsValidation()
    {
        var authService = new StubAuthService
        {
            PasswordChangeResult = new PasswordChangeResult
            {
                Succeeded = false,
                ValidationErrors = new Dictionary<string, string[]>
                {
                    ["PasswordTooShort"] = ["Passwords must be at least 6 characters."]
                }
            }
        };
        var controller = CreateController(authService);

        var result = await controller.ChangePasswordAsync(
            new AccountPasswordChangeRequest
            {
                CurrentPassword = "old-password",
                NewPassword = "short"
            },
            CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var objectResult = result as ObjectResult;
        Assert.That(objectResult?.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }

    private static AuthController CreateController(IAuthService authService)
    {
        return new AuthController(authService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], "test"))
                }
            }
        };
    }

    private sealed class StubAuthService : IAuthService
    {
        public LoginResult LoginResult { get; set; } = new();

        public AuthStatusResponse CurrentUserResponse { get; set; } = new();

        public ProfileUpdateResult UpdateResult { get; set; } = new();

        public PasswordChangeResult PasswordChangeResult { get; set; } = new();

        public Task<PasswordChangeResult> ChangePasswordAsync(
            ClaimsPrincipal principal,
            AccountPasswordChangeCommand command,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PasswordChangeResult);
        }

        public Task<AuthStatusResponse> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CurrentUserResponse);
        }

        public Task<LoginResult> LoginAsync(AuthLoginCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(LoginResult);
        }

        public Task SignOutAsync()
        {
            return Task.CompletedTask;
        }

        public Task<ProfileUpdateResult> UpdateCurrentUserAsync(
            ClaimsPrincipal principal,
            AccountProfileUpdateCommand command,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UpdateResult);
        }
    }
}
