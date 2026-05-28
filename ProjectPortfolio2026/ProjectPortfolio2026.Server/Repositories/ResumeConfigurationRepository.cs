using Microsoft.EntityFrameworkCore;
using ProjectPortfolio2026.Server.Data;
using ProjectPortfolio2026.Server.Domain.Portfolio;
using System.Data;

namespace ProjectPortfolio2026.Server.Repositories;

public sealed class ResumeConfigurationRepository(PortfolioDbContext dbContext) : IResumeConfigurationRepository
{
    public async Task<ResumeConfiguration?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ResumeConfigurations
            .OrderByDescending(configuration => configuration.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ResumeConfiguration> SaveAsync(ResumeConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        await using (transaction)
        {
        var existingConfiguration = await dbContext.ResumeConfigurations
            .OrderByDescending(record => record.Id)
            .FirstOrDefaultAsync(cancellationToken);

            if (existingConfiguration is null)
            {
                dbContext.ResumeConfigurations.Add(configuration);
            }
            else
            {
                existingConfiguration.SourceType = configuration.SourceType;
                existingConfiguration.SourceUrl = configuration.SourceUrl;
                existingConfiguration.DisplayLabel = configuration.DisplayLabel;
                existingConfiguration.Summary = configuration.Summary;
                configuration = existingConfiguration;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return configuration;
        }
    }
}
