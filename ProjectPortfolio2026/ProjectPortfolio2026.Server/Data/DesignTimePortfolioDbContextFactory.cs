using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProjectPortfolio2026.Server.Data;

public sealed class DesignTimePortfolioDbContextFactory : IDesignTimeDbContextFactory<PortfolioDbContext>
{
    public PortfolioDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PortfolioDbContext>();
        var connectionString =
            "Server=(localdb)\\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\\ProjectPortfolio2026.DesignTime.mdf;Database=ProjectPortfolio2026DesignTime;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True";

        AppDomain.CurrentDomain.SetData("DataDirectory", AppContext.BaseDirectory);
        optionsBuilder.UseSqlServer(connectionString);

        return new PortfolioDbContext(optionsBuilder.Options);
    }
}
