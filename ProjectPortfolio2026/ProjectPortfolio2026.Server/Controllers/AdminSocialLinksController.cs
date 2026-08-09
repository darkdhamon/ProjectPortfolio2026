using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectPortfolio2026.Server.Contracts.Admin;
using ProjectPortfolio2026.Server.Domain.Identity;
using ProjectPortfolio2026.Server.Domain.Portfolio;
using ProjectPortfolio2026.Server.Repositories;

namespace ProjectPortfolio2026.Server.Controllers;

[ApiController]
[Route("api/admin/social-links")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminSocialLinksController(IPortfolioProfileRepository portfolioProfileRepository) : ControllerBase
{
    private const int MaxHandleLength = 150;
    private const int MaxLabelLength = 100;
    private const int MaxPlatformLength = 50;
    private const int MaxSummaryLength = 500;
    private const int MaxUrlLength = 500;

    [HttpGet]
    [ProducesResponseType<List<AdminSocialLinkResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AdminSocialLinkResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        var socialLinks = await portfolioProfileRepository.GetSocialLinksAsync(cancellationToken);
        var response = socialLinks
            .OrderBy(link => link.SortOrder)
            .ThenBy(link => link.Label)
            .Select(link => link.ToResponse())
            .ToList();

        return Ok(response);
    }

    [HttpPut]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<List<AdminSocialLinkResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<AdminSocialLinkResponse>>> UpdateAsync(
        [FromBody] AdminSocialLinksUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateSocialLinks(request.SocialLinks);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(validationErrors.ToDictionary(pair => pair.Key, pair => pair.Value)));
        }

        var payloadLinks = request.SocialLinks
            .Select((link, index) => link.ToSocialLink(index + 1))
            .ToList();

        var savedSocialLinks = await portfolioProfileRepository.SaveSocialLinksAsync(payloadLinks, cancellationToken);
        var response = savedSocialLinks
            .OrderBy(link => link.SortOrder)
            .ThenBy(link => link.Label)
            .Select(link => link.ToResponse())
            .ToList();

        return Ok(response);
    }

    private static Dictionary<string, string[]> ValidateSocialLinks(List<AdminSocialLinkRequest> links)
    {
        var errors = new Dictionary<string, string[]>();

        for (var index = 0; index < links.Count; index++)
        {
            var link = links[index];
            var entryKey = $"socialLinks[{index}]";

            var platform = link.Platform?.Trim();
            if (string.IsNullOrWhiteSpace(platform))
            {
                errors[$"{entryKey}.platform"] = ["A platform is required for each social link."];
            }
            else if (platform.Length > MaxPlatformLength)
            {
                errors[$"{entryKey}.platform"] = ["A platform must be 50 characters or fewer."];
            }

            var label = link.Label?.Trim();
            if (string.IsNullOrWhiteSpace(label))
            {
                errors[$"{entryKey}.label"] = ["A label is required for each social link."];
            }
            else if (label.Length > MaxLabelLength)
            {
                errors[$"{entryKey}.label"] = ["A label must be 100 characters or fewer."];
            }

            var url = link.Url?.Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                errors[$"{entryKey}.url"] = ["A valid http or https URL is required for each social link."];
            }
            else if (url.Length > MaxUrlLength)
            {
                errors[$"{entryKey}.url"] = ["A URL must be 500 characters or fewer."];
            }
            else if (!IsValidAbsoluteHttpUrl(url))
            {
                errors[$"{entryKey}.url"] = ["A valid http or https URL is required for each social link."];
            }

            var handle = link.Handle?.Trim();
            if (!string.IsNullOrWhiteSpace(handle) && handle.Length > MaxHandleLength)
            {
                errors[$"{entryKey}.handle"] = ["A handle must be 150 characters or fewer."];
            }

            var summary = link.Summary?.Trim();
            if (!string.IsNullOrWhiteSpace(summary) && summary.Length > MaxSummaryLength)
            {
                errors[$"{entryKey}.summary"] = ["A summary must be 500 characters or fewer."];
            }
        }

        return errors;
    }

    private static bool IsValidAbsoluteHttpUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
    }
}

file static class AdminSocialLinksControllerExtensions
{
    public static AdminSocialLinkResponse ToResponse(this PortfolioSocialLink socialLink)
    {
        return new AdminSocialLinkResponse
        {
            Id = socialLink.Id,
            Platform = socialLink.Platform,
            Label = socialLink.Label,
            Url = socialLink.Url,
            Handle = socialLink.Handle,
            Summary = socialLink.Summary,
            SortOrder = socialLink.SortOrder,
            IsVisible = socialLink.IsVisible
        };
    }

    public static PortfolioSocialLink ToSocialLink(this AdminSocialLinkRequest request, int fallbackSortOrder)
    {
        var trimmedHandle = request.Handle?.Trim();
        var trimmedLabel = request.Label?.Trim();
        var trimmedPlatform = request.Platform?.Trim();
        var trimmedSummary = request.Summary?.Trim();
        var trimmedUrl = request.Url?.Trim();

        return new PortfolioSocialLink
        {
            Id = request.Id ?? 0,
            Platform = trimmedPlatform ?? string.Empty,
            Label = trimmedLabel ?? string.Empty,
            Url = trimmedUrl ?? string.Empty,
            Handle = trimmedHandle,
            Summary = trimmedSummary,
            SortOrder = request.SortOrder ?? fallbackSortOrder,
            IsVisible = request.IsVisible ?? true
        };
    }
}
