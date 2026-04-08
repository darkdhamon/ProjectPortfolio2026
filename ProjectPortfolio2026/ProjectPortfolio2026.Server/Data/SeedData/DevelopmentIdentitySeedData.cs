using Microsoft.AspNetCore.Identity;
using ProjectPortfolio2026.Server.Domain.Identity;

namespace ProjectPortfolio2026.Server.Data.SeedData;

public static class DevelopmentIdentitySeedData
{
    public const string SeedUserName = "admin";
    public const string SeedEmail = "admin@example.com";
    public const string SeedPassword = "Password@1";

    public static async Task InitializeAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        if (!await roleManager.RoleExistsAsync(RoleNames.Admin))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(RoleNames.Admin));
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException("Unable to create the development Admin role.");
            }
        }

        var user = await userManager.FindByNameAsync(SeedUserName);
        user ??= await userManager.FindByEmailAsync(SeedEmail);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = SeedUserName,
                Email = SeedEmail,
                DisplayName = null,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, SeedPassword);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException("Unable to create the development admin user.");
            }
        }
        else if (!await userManager.HasPasswordAsync(user))
        {
            var addPasswordResult = await userManager.AddPasswordAsync(user, SeedPassword);
            if (!addPasswordResult.Succeeded)
            {
                throw new InvalidOperationException("Unable to assign the development admin password.");
            }
        }

        if (!await userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            var addToRoleResult = await userManager.AddToRoleAsync(user, RoleNames.Admin);
            if (!addToRoleResult.Succeeded)
            {
                throw new InvalidOperationException("Unable to assign the Admin role to the development admin user.");
            }
        }
    }
}
