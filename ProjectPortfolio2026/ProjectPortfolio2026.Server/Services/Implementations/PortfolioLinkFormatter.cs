using System.Net.Mail;
using ProjectPortfolio2026.Server.Domain.Portfolio;
using ProjectPortfolio2026.Server.Services.Interfaces;

namespace ProjectPortfolio2026.Server.Services.Implementations;

public sealed class PortfolioLinkFormatter : IPortfolioLinkFormatter
{
    public string? BuildContactMethodHref(PortfolioContactMethod contactMethod)
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

    public string? BuildSocialLinkUrl(PortfolioSocialLink socialLink)
    {
        return SanitizeUrl(socialLink.Url, "http", "https");
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
