using ProjectPortfolio2026.Server.Contracts.Portfolio;
using ProjectPortfolio2026.Server.Domain.Portfolio;

namespace ProjectPortfolio2026.Server.Mappers;

public static class PortfolioProfileContractMapper
{
    public static PortfolioProfileResponse ToResponse(this PortfolioProfile profile, string? requestId = null)
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
                .Select(contactMethod => new PortfolioContactMethodResponse
                {
                    Type = contactMethod.Type,
                    Label = contactMethod.Label,
                    Value = contactMethod.Value,
                    Href = contactMethod.Href,
                    Note = contactMethod.Note,
                    SortOrder = contactMethod.SortOrder
                })
                .ToList(),
            SocialLinks = profile.SocialLinks
                .Where(socialLink => socialLink.IsVisible)
                .OrderBy(socialLink => socialLink.SortOrder)
                .ThenBy(socialLink => socialLink.Label)
                .Select(socialLink => new PortfolioSocialLinkResponse
                {
                    Platform = socialLink.Platform,
                    Label = socialLink.Label,
                    Url = socialLink.Url,
                    Handle = socialLink.Handle,
                    Summary = socialLink.Summary,
                    SortOrder = socialLink.SortOrder
                })
                .ToList()
        };
    }
}
