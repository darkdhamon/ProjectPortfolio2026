namespace ProjectPortfolio2026.ResumeParser.Models;

public sealed class ResumeProjectEntry
{
    public string? Title { get; init; }

    public ResumeDateRange Dates { get; init; } = new();

    public List<string> DescriptionLines { get; init; } = [];

    public string? DescriptionMarkdown { get; init; }

    public List<string> Skills { get; init; } = [];

    public List<string> Technologies { get; init; } = [];

    public string? Url { get; init; }

    public string? RepositoryUrl { get; init; }

    public string? RawText { get; init; }
}
