using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProjectPortfolio2026.Server.Data;

public sealed class DesignTimePortfolioDbContextFactory : IDesignTimeDbContextFactory<PortfolioDbContext>
{
    public PortfolioDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PortfolioDbContext>();
        var dataDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "App_Data"));
        Directory.CreateDirectory(dataDirectory);
        var databaseFilePath = Path.Combine(dataDirectory, "ProjectPortfolio2026.DesignTime.mdf");
        var connectionString =
            $"Server=(localdb)\\MSSQLLocalDB;AttachDbFilename={databaseFilePath};Database=ProjectPortfolio2026DesignTime;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        optionsBuilder.UseSqlServer(connectionString);

        return new PortfolioDbContext(optionsBuilder.Options);
    }
}
