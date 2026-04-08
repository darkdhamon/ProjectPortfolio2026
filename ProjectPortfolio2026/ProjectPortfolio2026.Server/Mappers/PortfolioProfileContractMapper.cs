using System.Net.Mail;
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
                .Select(ToContactMethodResponse)
                .Where(response => response is not null)
                .Select(response => response!)
                .ToList(),
            SocialLinks = profile.SocialLinks
                .Where(socialLink => socialLink.IsVisible)
                .OrderBy(socialLink => socialLink.SortOrder)
                .ThenBy(socialLink => socialLink.Label)
                .Select(ToSocialLinkResponse)
                .Where(response => response is not null)
                .Select(response => response!)
                .ToList()
        };
    }

    private static PortfolioContactMethodResponse? ToContactMethodResponse(PortfolioContactMethod contactMethod)
    {
        var href = BuildContactMethodHref(contactMethod);
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

    private static PortfolioSocialLinkResponse? ToSocialLinkResponse(PortfolioSocialLink socialLink)
    {
        var url = SanitizeUrl(socialLink.Url, "http", "https");
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

    private static string? BuildContactMethodHref(PortfolioContactMethod contactMethod)
    {
        if (contactMethod.Type.Equals("email", StringComparison.OrdinalIgnoreCase))
        {
            return BuildMailToHref(contactMethod.Value);
        }

        if (contactMethod.Type.Equals("phone", StringComparison.OrdinalIgnoreCase))
        {
            return BuildTelephoneHref(contactMethod.Value);
        }

        return SanitizeUrl(contactMethod.Href, "http", "https");
    }

    private static string? BuildMailToHref(string value)
    {
        var trimmedValue = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmedValue))
        {
            return null;
        }

        try
        {
            var mailAddress = new MailAddress(trimmedValue);
            return mailAddress.Address.Equals(trimmedValue, StringComparison.OrdinalIgnoreCase)
                ? $"mailto:{mailAddress.Address}"
                : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string? BuildTelephoneHref(string value)
    {
        var normalizedPhone = NormalizeTelephoneValue(value);
        return string.IsNullOrWhiteSpace(normalizedPhone)
            ? null
            : $"tel:{normalizedPhone}";
    }

    private static string? NormalizeTelephoneValue(string value)
    {
        var trimmedValue = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmedValue))
        {
            return null;
        }

        var characters = trimmedValue
            .Where((character, index) => char.IsDigit(character) || (character == '+' && index == 0))
            .ToArray();
        if (characters.Length == 0)
        {
            return null;
        }

        var normalizedValue = new string(characters);
        return normalizedValue.Any(char.IsDigit)
            ? normalizedValue
            : null;
    }

    private static string? SanitizeUrl(string? value, params string[] allowedSchemes)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            return null;
        }

        return allowedSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase)
            ? uri.AbsoluteUri
            : null;
    }
}
