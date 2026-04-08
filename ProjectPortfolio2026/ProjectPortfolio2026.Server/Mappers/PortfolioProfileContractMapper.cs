using ProjectPortfolio2026.Server.Contracts.Portfolio;
using ProjectPortfolio2026.Server.Domain.Portfolio;
using ProjectPortfolio2026.Server.Services.Interfaces;

namespace ProjectPortfolio2026.Server.Mappers;

public static class PortfolioProfileContractMapper
{
    public static PortfolioProfileResponse ToResponse(
        this PortfolioProfile profile,
        IPortfolioLinkFormatter linkFormatter,
        string? requestId = null)
    {
        return new PortfolioProfileResponse
        {
            RequestId = requestId,
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            ContactHeadline = profile.ContactHeadline,
            ContactIntro = profile.ContactIntro,
            AvailabilityHeadline = profile.AvailabilityHeadline,
            AvailabilitySummary = profile.AvailabilitySummary,
            ContactMethods = profile.ContactMethods
                .Where(contactMethod => contactMethod.IsVisible)
                .OrderBy(contactMethod => contactMethod.SortOrder)
                .ThenBy(contactMethod => contactMethod.Label)
                .Select(contactMethod => ToContactMethodResponse(contactMethod, linkFormatter))
                .Where(response => response is not null)
                .Select(response => response!)
                .ToList(),
            SocialLinks = profile.SocialLinks
                .Where(socialLink => socialLink.IsVisible)
                .OrderBy(socialLink => socialLink.SortOrder)
                .ThenBy(socialLink => socialLink.Label)
                .Select(socialLink => ToSocialLinkResponse(socialLink, linkFormatter))
                .Where(response => response is not null)
                .Select(response => response!)
                .ToList()
        };
    }

    private static PortfolioContactMethodResponse? ToContactMethodResponse(
        PortfolioContactMethod contactMethod,
        IPortfolioLinkFormatter linkFormatter)
    {
        var href = linkFormatter.BuildContactMethodHref(contactMethod);
        return new PortfolioContactMethodResponse
        {
            Type = contactMethod.Type,
            Label = contactMethod.Label,
            Value = contactMethod.Value,
            Href = href,
            Note = contactMethod.Note,
            SortOrder = contactMethod.SortOrder
        };
    }

    private static PortfolioSocialLinkResponse? ToSocialLinkResponse(
        PortfolioSocialLink socialLink,
        IPortfolioLinkFormatter linkFormatter)
    {
        var url = linkFormatter.BuildSocialLinkUrl(socialLink);
        if (url is null)
        {
            return null;
        }

        return new PortfolioSocialLinkResponse
        {
            Platform = socialLink.Platform,
            Label = socialLink.Label,
            Url = url,
            Handle = socialLink.Handle,
            Summary = socialLink.Summary,
            SortOrder = socialLink.SortOrder
        };
    }
}
