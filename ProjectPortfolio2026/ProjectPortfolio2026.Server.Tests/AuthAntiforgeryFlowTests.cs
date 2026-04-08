using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Contracts.Auth;
using ProjectPortfolio2026.Server.Controllers;
using ProjectPortfolio2026.Server.Infrastructure.Security;
using ProjectPortfolio2026.Server.Services.Interfaces;
using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class AuthAntiforgeryFlowTests
{
    [Test]
    public async Task ChangePasswordAsync_ReturnsBadRequest_WhenAntiforgeryTokenIsMissing()
    {
        await using var host = await CreateHostAsync();
        using var client = host.GetTestClient();

        var cookies = await GetIssuedCookiesAsync(client);
        using var request = CreatePasswordChangeRequest(cookies);

        var response = await client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task ChangePasswordAsync_Succeeds_WhenAntiforgeryTokenIsProvided()
    {
        await using var host = await CreateHostAsync();
        using var client = host.GetTestClient();

        var cookies = await GetIssuedCookiesAsync(client);
        var requestToken = ExtractCookieValue(cookies, AntiforgeryCookieManager.RequestTokenCookieName);
        using var request = CreatePasswordChangeRequest(cookies);
        request.Headers.Add(AntiforgeryCookieManager.HeaderName, requestToken);

        var response = await client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    private static async Task<WebApplication> CreateHostAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(AuthController).Assembly);
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = AntiforgeryCookieManager.HeaderName;
            options.Cookie.Name = "ProjectPortfolio2026.Antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });
        builder.Services
            .AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IAuthService, StubAuthService>();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();

        return app;
    }

    private static async Task<string> GetIssuedCookiesAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/auth/me");
        response.EnsureSuccessStatusCode();

        return string.Join("; ", response.Headers.GetValues("Set-Cookie").Select(ParseCookieHeader));
    }

    private static HttpRequestMessage CreatePasswordChangeRequest(string cookies)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/me/password")
        {
            Content = new StringContent(
                """
                {"currentPassword":"old-password","newPassword":"Passw0rd!"}
                """,
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("Cookie", cookies);
        return request;
    }

    private static string ExtractCookieValue(string cookieHeader, string cookieName)
    {
        foreach (var cookiePart in cookieHeader.Split("; ", StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = cookiePart.IndexOf('=');
            if (separatorIndex < 0)
            {
                continue;
            }

            var currentCookieName = cookiePart[..separatorIndex];
            if (string.Equals(currentCookieName, cookieName, StringComparison.Ordinal))
            {
                return Uri.UnescapeDataString(cookiePart[(separatorIndex + 1)..]);
            }
        }

        Assert.Fail($"Cookie '{cookieName}' was not issued.");
        return string.Empty;
    }

    private static string ParseCookieHeader(string setCookieHeader)
    {
        var separatorIndex = setCookieHeader.IndexOf(';');
        return separatorIndex >= 0 ? setCookieHeader[..separatorIndex] : setCookieHeader;
    }

    private sealed class StubAuthService : IAuthService
    {
        public Task<PasswordChangeResult> ChangePasswordAsync(
            ClaimsPrincipal principal,
            AccountPasswordChangeCommand command,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PasswordChangeResult { Succeeded = true });
        }

        public Task<AuthStatusResponse> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuthStatusResponse
            {
                IsAuthenticated = true,
                IsAdmin = true,
                UserName = "admin"
            });
        }

        public Task<LoginResult> LoginAsync(AuthLoginCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new LoginResult
            {
                Succeeded = true,
                User = new AuthStatusResponse
                {
                    IsAuthenticated = true,
                    IsAdmin = true,
                    UserName = "admin"
                }
            });
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
            return Task.FromResult(new ProfileUpdateResult
            {
                Succeeded = true,
                User = new AuthStatusResponse
                {
                    IsAuthenticated = true,
                    IsAdmin = true,
                    UserName = command.UserName
                }
            });
        }
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, "admin"),
                    new Claim(ClaimTypes.Role, "portfolioAdmin")
                ],
                Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
