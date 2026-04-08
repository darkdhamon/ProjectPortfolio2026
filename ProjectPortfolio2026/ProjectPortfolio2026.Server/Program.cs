using Microsoft.EntityFrameworkCore;
using ProjectPortfolio2026.Server.Data;
using ProjectPortfolio2026.Server.Data.SeedData;
using ProjectPortfolio2026.Server.Infrastructure.RequestTracking;
using ProjectPortfolio2026.Server.Repositories;

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
builder.Services.AddDbContext<PortfolioDbContext>(options => options.UseSqlServer(resolvedConnectionString));
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
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
    }
}
