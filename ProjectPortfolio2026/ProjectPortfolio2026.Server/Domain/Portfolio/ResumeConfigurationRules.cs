namespace ProjectPortfolio2026.Server.Domain.Portfolio;

public static class ResumeConfigurationRules
{
    public static IReadOnlyDictionary<string, string[]> Validate(string? sourceType, string? sourceUrl, string? displayLabel)
    {
        var normalizedSourceType = ResumeSourceTypes.Normalize(sourceType);
        var trimmedSourceUrl = NormalizeText(sourceUrl);
        var trimmedDisplayLabel = NormalizeText(displayLabel);
        var errors = new Dictionary<string, string[]>();

        if (!ResumeSourceTypes.IsSupported(sourceType))
        {
            errors["sourceType"] = ["Select a supported resume source type."];
        }

        if (normalizedSourceType == ResumeSourceTypes.None)
        {
            return errors;
        }

        if (string.IsNullOrWhiteSpace(trimmedSourceUrl))
        {
            errors["sourceUrl"] = ["A resume source URL is required when a hosted or embedded source is enabled."];
        }
        else if (!IsAbsoluteHttpUrl(trimmedSourceUrl))
        {
            errors["sourceUrl"] = ["Resume source URLs must use an absolute http or https address."];
        }

        if (string.IsNullOrWhiteSpace(trimmedDisplayLabel))
        {
            errors["displayLabel"] = ["A public display label is required when a resume source is enabled."];
        }

        return errors;
    }

    public static bool HasCompletePublicConfiguration(ResumeConfiguration? configuration)
    {
        if (configuration is null)
        {
            return false;
        }

        return Validate(configuration.SourceType, configuration.SourceUrl, configuration.DisplayLabel).Count == 0
            && ResumeSourceTypes.Normalize(configuration.SourceType) != ResumeSourceTypes.None;
    }

    public static bool IsAbsoluteHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
    }

    public static string? NormalizeText(string? value)
    {
        var trimmedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmedValue) ? null : trimmedValue;
    }
}
