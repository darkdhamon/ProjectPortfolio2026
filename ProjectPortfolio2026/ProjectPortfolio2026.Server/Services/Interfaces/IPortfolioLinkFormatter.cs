using ProjectPortfolio2026.Server.Domain.Portfolio;

namespace ProjectPortfolio2026.Server.Services.Interfaces;

public interface IPortfolioLinkFormatter
{
    string? BuildContactMethodHref(PortfolioContactMethod contactMethod);

    string? BuildSocialLinkUrl(PortfolioSocialLink socialLink);
}
