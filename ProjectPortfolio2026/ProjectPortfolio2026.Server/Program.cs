using Microsoft.EntityFrameworkCore;
using ProjectPortfolio2026.Server.Data;
using ProjectPortfolio2026.Server.Data.SeedData;
using ProjectPortfolio2026.Server.Repositories;

var builder = WebApplication.CreateBuilder(args);
var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);

var connectionString = builder.Configuration.GetConnectionString("PortfolioDatabase");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'PortfolioDatabase' is required. Configure it in appsettings, app secrets, or environment variables.");
}

var resolvedConnectionString = connectionString.Replace(
    "|DataDirectory|",
    dataDirectory,
    StringComparison.OrdinalIgnoreCase);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<PortfolioDbContext>(options => options.UseSqlServer(resolvedConnectionString));
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
    dbContext.Database.Migrate();

    if (app.Environment.IsDevelopment())
    {
        await PortfolioSeedData.InitializeAsync(dbContext);
    }
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
