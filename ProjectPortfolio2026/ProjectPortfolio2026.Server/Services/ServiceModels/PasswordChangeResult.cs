namespace ProjectPortfolio2026.Server.Services.ServiceModels;

public sealed class PasswordChangeResult
{
    public bool Succeeded { get; init; }

    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; init; } = new Dictionary<string, string[]>();
}
