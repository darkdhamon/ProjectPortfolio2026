namespace ProjectPortfolio2026.ResumeParser.Models;

public sealed class ResumeCustomSection
{
    public string? Title { get; init; }

    public List<string> Items { get; init; } = [];

    public string? RawText { get; init; }
}
