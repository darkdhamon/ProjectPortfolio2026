using Microsoft.AspNetCore.Http;
using ProjectPortfolio2026.Server.Domain.Portfolio;
using ProjectPortfolio2026.Server.Repositories;
using ProjectPortfolio2026.Server.Services.Interfaces;
using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Services.Implementations;

public sealed class ResumeImportService(
    IResumeImportFileStore resumeImportFileStore,
    IResumeParserService resumeParserService,
    IResumeConfigurationRepository resumeConfigurationRepository,
    HttpClient httpClient) : IResumeImportService
{
    public async Task<ResumeImportCandidateResult> ParseAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        await using var stagedFile = await resumeImportFileStore.StageAsync(file, cancellationToken);
        await using var content = File.OpenRead(stagedFile.StoredFilePath);

        var result = await resumeParserService.ParseAsync(content, stagedFile.OriginalFileName, cancellationToken);
        result.SourceFileName ??= stagedFile.OriginalFileName;

        return ResumeImportCandidateNormalizer.Normalize(result);
    }

    public async Task<ResumeImportCandidateResult> ParseConfiguredSourceAsync(CancellationToken cancellationToken = default)
    {
        var configuration = await resumeConfigurationRepository.GetAsync(cancellationToken)
            ?? throw new ResumeImportValidationException("No resume configuration is available.");

        if (!ResumeConfigurationRules.HasCompletePublicConfiguration(configuration))
        {
            throw new ResumeImportValidationException("A complete resume source configuration is required before parsing.");
        }

        var sourceUrl = configuration.SourceUrl?.Trim();
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var sourceUri))
        {
            throw new ResumeImportValidationException("The configured resume source URL is invalid.");
        }

        if (sourceUri.Scheme != Uri.UriSchemeHttp && sourceUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ResumeImportValidationException("Only http and https configured resume source URLs are supported.");
        }

        using var response = await httpClient.GetAsync(sourceUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new ResumeImportValidationException($"The configured resume source returned {(int)response.StatusCode}.");
        }

        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var stagedCopy = new MemoryStream();
        await content.CopyToAsync(stagedCopy, cancellationToken);
        stagedCopy.Position = 0;

        var stageFile = new FormFile(
            stagedCopy,
            0,
            stagedCopy.Length,
            "configured-source",
            GetConfiguredSourceFileName(sourceUri, response))
        {
            Headers = new HeaderDictionary(),
            ContentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty
        };

        return await ParseAsync(stageFile, cancellationToken);
    }

    private static string GetConfiguredSourceFileName(Uri sourceUri, HttpResponseMessage response)
    {
        var contentDispositionFileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"');
        if (!string.IsNullOrWhiteSpace(contentDispositionFileName))
        {
            return contentDispositionFileName;
        }

        var path = sourceUri.AbsolutePath;
        var lastSegment = Path.GetFileName(path);
        return string.IsNullOrWhiteSpace(lastSegment)
            ? "configured-resume"
            : lastSegment;
    }
}
