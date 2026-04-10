using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectPortfolio2026.Server.Data;
using ProjectPortfolio2026.Server.Data.SeedData;
using ProjectPortfolio2026.Server.Domain.Identity;
using ProjectPortfolio2026.Server.Infrastructure.RequestTracking;
using ProjectPortfolio2026.Server.Infrastructure.Security;
using ProjectPortfolio2026.Server.Repositories;
using ProjectPortfolio2026.Server.Services.Implementations;
using ProjectPortfolio2026.Server.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PortfolioDatabase");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'PortfolioDatabase' is required. Configure it in appsettings, app secrets, or environment variables.");
}

var resolvedConnectionString = ConnectionStringPathResolver.ResolveDataPaths(
    connectionString,
    builder.Environment.ContentRootPath);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<RequestTrackingFilter>();
});
builder.Services.AddOpenApi();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = AntiforgeryCookieManager.HeaderName;
    options.Cookie.Name = "ProjectPortfolio2026.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddDbContext<PortfolioDbContext>(options => options.UseSqlServer(resolvedConnectionString));
builder.Services
    .AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<PortfolioDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IPortfolioProfileRepository, PortfolioProfileRepository>();
builder.Services.AddScoped<IPortfolioLinkFormatter, PortfolioLinkFormatter>();
builder.Services.AddScoped<IProjectTagNormalizer, ProjectTagNormalizer>();
builder.Services.AddScoped<IFeaturedProjectSelector, FeaturedProjectSelector>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IEmployerRepository, EmployerRepository>();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IResumeParserService, DeferredResumeParserService>();
builder.Services.AddScoped<RequestTrackingFilter>();

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await EnsureDatabaseReadyAsync(app.Services, connectionString, resolvedConnectionString, app.Environment.IsDevelopment());

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();

static async Task EnsureDatabaseReadyAsync(
    IServiceProvider services,
    string connectionString,
    string resolvedConnectionString,
    bool isDevelopment)
{
    try
    {
        await MigrateAndSeedAsync(services, isDevelopment);
    }
    catch (Microsoft.Data.SqlClient.SqlException exception)
        when (LocalDbDatabaseRecovery.CanRecover(connectionString, exception.Number, exception.Message) ||
              LocalDbDatabaseRecovery.CanRecover(resolvedConnectionString, exception.Number, exception.Message))
    {
        var recoveryConnectionString = LocalDbDatabaseRecovery.CanRecover(resolvedConnectionString, exception.Number, exception.Message)
            ? resolvedConnectionString
            : connectionString;
        var recovered = await LocalDbDatabaseRecovery.TryRecoverAsync(recoveryConnectionString);
        if (!recovered)
        {
            throw;
        }

        await MigrateAndSeedAsync(services, isDevelopment);
    }
}

static async Task MigrateAndSeedAsync(IServiceProvider services, bool isDevelopment)
{
    await using var scope = services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
    await dbContext.Database.MigrateAsync();

    if (isDevelopment)
    {
        await PortfolioSeedData.InitializeAsync(dbContext);
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await DevelopmentIdentitySeedData.InitializeAsync(roleManager, userManager);
    }
}

public partial class Program;
