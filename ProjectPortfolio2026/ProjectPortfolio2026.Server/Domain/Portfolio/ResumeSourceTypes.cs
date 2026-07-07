namespace ProjectPortfolio2026.Server.Domain.Portfolio;

public static class ResumeSourceTypes
{
    public const string None = "none";
    public const string HostedFile = "hosted-file";
    public const string Embed = "embed";

    public static bool IsSupported(string? value)
    {
        var normalizedValue = value?.Trim().ToLowerInvariant();
        return normalizedValue is None or HostedFile or Embed;
    }

    public static string Normalize(string? value)
    {
        var normalizedValue = value?.Trim().ToLowerInvariant();

        return normalizedValue switch
        {
            HostedFile => HostedFile,
            Embed => Embed,
            _ => None
        };
    }
}
