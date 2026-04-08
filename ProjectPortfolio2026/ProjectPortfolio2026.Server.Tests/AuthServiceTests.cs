using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Contracts.Auth;
using ProjectPortfolio2026.Server.Data;
using ProjectPortfolio2026.Server.Data.SeedData;
using ProjectPortfolio2026.Server.Domain.Identity;
using ProjectPortfolio2026.Server.Services.Implementations;
using System.Security.Claims;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class AuthServiceTests
{
    [Test]
    public async Task LoginAsync_AcceptsMatchingEmailAddress()
    {
        using var dbContext = CreateDbContext();
        var userManager = CreateUserManager(dbContext);
        var signInManager = CreateSignInManager(userManager);
        var service = new AuthService(userManager, signInManager);

        var user = new ApplicationUser
        {
            UserName = "portfolio-admin",
            Email = "admin@example.com"
        };

        await userManager.CreateAsync(user, "Passw0rd!");
        dbContext.Roles.Add(new IdentityRole
        {
            Name = RoleNames.Admin,
            NormalizedName = RoleNames.Admin.ToUpperInvariant()
        });
        await dbContext.SaveChangesAsync();
        await userManager.AddToRoleAsync(user, RoleNames.Admin);

        var result = await service.LoginAsync(
            new AuthLoginRequest
            {
                Login = "admin@example.com",
                Password = "Passw0rd!"
            });

        Assert.That(result.Succeeded, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(result.User?.IsAuthenticated, Is.True);
            Assert.That(result.User?.IsAdmin, Is.True);
            Assert.That(result.User?.Email, Is.EqualTo("admin@example.com"));
        });
    }

    [Test]
    public async Task UpdateCurrentUserAsync_ReturnsUserNameConflict_WhenAnotherUserAlreadyUsesRequestedName()
    {
        using var dbContext = CreateDbContext();
        var userManager = CreateUserManager(dbContext);
        var signInManager = CreateSignInManager(userManager);
        var service = new AuthService(userManager, signInManager);

        var currentUser = new ApplicationUser
        {
            UserName = "current-user",
            Email = "current@example.com"
        };
        var existingUser = new ApplicationUser
        {
            UserName = "taken-user",
            Email = "taken@example.com"
        };

        await userManager.CreateAsync(currentUser, "Passw0rd!");
        await userManager.CreateAsync(existingUser, "Passw0rd!");

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, currentUser.Id)
        ], "test"));

        var result = await service.UpdateCurrentUserAsync(
            principal,
            new AccountProfileUpdateRequest
            {
                UserName = "taken-user"
            });

        Assert.That(result.UserNameConflict, Is.True);
        Assert.That(result.Succeeded, Is.False);
    }

    [Test]
    public async Task GetCurrentUserAsync_FallsBackToUserName_WhenDisplayNameIsEmpty()
    {
        using var dbContext = CreateDbContext();
        var userManager = CreateUserManager(dbContext);
        var signInManager = CreateSignInManager(userManager);
        var service = new AuthService(userManager, signInManager);

        var user = new ApplicationUser
        {
            UserName = "current-user",
            Email = "current@example.com",
            DisplayName = null
        };

        await userManager.CreateAsync(user, "Passw0rd!");
        dbContext.Roles.Add(new IdentityRole
        {
            Name = RoleNames.Admin,
            NormalizedName = RoleNames.Admin.ToUpperInvariant()
        });
        await dbContext.SaveChangesAsync();
        await userManager.AddToRoleAsync(user, RoleNames.Admin);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        ], "test"));

        var response = await service.GetCurrentUserAsync(principal);

        Assert.Multiple(() =>
        {
            Assert.That(response.IsAuthenticated, Is.True);
            Assert.That(response.DisplayName, Is.EqualTo("current-user"));
            Assert.That(response.IsAdmin, Is.True);
        });
    }

    [Test]
    public async Task DevelopmentIdentitySeedData_InitializesDefaultAdminPassword()
    {
        using var dbContext = CreateDbContext();
        var userManager = CreateUserManager(dbContext);
        var roleManager = CreateRoleManager(dbContext);

        await DevelopmentIdentitySeedData.InitializeAsync(roleManager, userManager);

        var user = await userManager.FindByNameAsync(DevelopmentIdentitySeedData.SeedUserName);

        Assert.That(user, Is.Not.Null);
        Assert.Multiple(async () =>
        {
            Assert.That(await userManager.CheckPasswordAsync(user!, DevelopmentIdentitySeedData.SeedPassword), Is.True);
            Assert.That(await userManager.IsInRoleAsync(user!, RoleNames.Admin), Is.True);
        });
    }

    [Test]
    public async Task DevelopmentIdentitySeedData_AddsDefaultPassword_WhenExistingAdminUserHasNoPassword()
    {
        using var dbContext = CreateDbContext();
        var userManager = CreateUserManager(dbContext);
        var roleManager = CreateRoleManager(dbContext);

        var user = new ApplicationUser
        {
            UserName = DevelopmentIdentitySeedData.SeedUserName,
            Email = DevelopmentIdentitySeedData.SeedEmail
        };

        await userManager.CreateAsync(user);
        await roleManager.CreateAsync(new IdentityRole(RoleNames.Admin));
        await userManager.AddToRoleAsync(user, RoleNames.Admin);

        await DevelopmentIdentitySeedData.InitializeAsync(roleManager, userManager);

        Assert.That(await userManager.CheckPasswordAsync(user, DevelopmentIdentitySeedData.SeedPassword), Is.True);
        Assert.That(await userManager.HasPasswordAsync(user), Is.True);
    }

    [Test]
    public async Task ChangePasswordAsync_RejectsNonEmptyCurrentPassword_WhenUserStartedWithoutPassword()
    {
        using var dbContext = CreateDbContext();
        var userManager = CreateUserManager(dbContext);
        var signInManager = CreateSignInManager(userManager);
        var service = new AuthService(userManager, signInManager);

        var user = new ApplicationUser
        {
            UserName = DevelopmentIdentitySeedData.SeedUserName,
            Email = DevelopmentIdentitySeedData.SeedEmail
        };

        await userManager.CreateAsync(user);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        ], "test"));

        var result = await service.ChangePasswordAsync(
            principal,
            new AccountPasswordChangeRequest
            {
                CurrentPassword = "wrong-value",
                NewPassword = "Passw0rd!"
            });

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.ValidationErrors.ContainsKey("CurrentPassword"), Is.True);
    }

    private static PortfolioDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new PortfolioDbContext(options);
    }

    private static UserManager<ApplicationUser> CreateUserManager(PortfolioDbContext dbContext)
    {
        var store = new UserStore<ApplicationUser, IdentityRole, PortfolioDbContext>(dbContext);
        var options = Options.Create(new IdentityOptions());

        return new UserManager<ApplicationUser>(
            store,
            options,
            new PasswordHasher<ApplicationUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            new Logger<UserManager<ApplicationUser>>(new LoggerFactory()));
    }

    private static SignInManager<ApplicationUser> CreateSignInManager(UserManager<ApplicationUser> userManager)
    {
        return new TestSignInManager(
            userManager,
            new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = new ServiceCollection()
                        .AddLogging()
                        .AddAuthentication()
                        .Services
                        .BuildServiceProvider()
                }
            },
            new UserClaimsPrincipalFactory<ApplicationUser>(userManager, Options.Create(new IdentityOptions())),
            Options.Create(new IdentityOptions()),
            new Logger<SignInManager<ApplicationUser>>(new LoggerFactory()),
            new AuthenticationSchemeProvider(Options.Create(new AuthenticationOptions())),
            new DefaultUserConfirmation<ApplicationUser>());
    }

    private static RoleManager<IdentityRole> CreateRoleManager(PortfolioDbContext dbContext)
    {
        var store = new RoleStore<IdentityRole, PortfolioDbContext>(dbContext);

        return new RoleManager<IdentityRole>(
            store,
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new Logger<RoleManager<IdentityRole>>(new LoggerFactory()));
    }

    private sealed class TestSignInManager : SignInManager<ApplicationUser>
    {
        public TestSignInManager(
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<ApplicationUser>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<ApplicationUser> confirmation)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
        }

        public override Task<SignInResult> PasswordSignInAsync(
            ApplicationUser user,
            string password,
            bool isPersistent,
            bool lockoutOnFailure)
        {
            return Task.FromResult(password is "Passw0rd!" or DevelopmentIdentitySeedData.SeedPassword
                ? SignInResult.Success
                : SignInResult.Failed);
        }

        public override Task RefreshSignInAsync(ApplicationUser user)
        {
            return Task.CompletedTask;
        }
    }

}
