using Microsoft.Extensions.DependencyInjection;

namespace ProjectPortfolio2026.Server.Infrastructure.RequestTracking;

public static class RequestTrackingMvcServiceCollectionExtensions
{
    public static IMvcBuilder AddProjectPortfolioControllers(this IServiceCollection services)
    {
        return services.AddControllersWithViews(options =>
        {
            options.Filters.Add<RequestTrackingFilter>();
        });
    }
}
