using ProjectPortfolio2026.Server.Services.Interfaces;
using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Services.Implementations;

public sealed class TemporaryResumeFileStore : IResumeImportFileStore
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".docx"
    };

    private readonly string stagingRootPath;

    public TemporaryResumeFileStore()
        : this(Path.Combine(Path.GetTempPath(), "ProjectPortfolio2026", "resume-imports"))
    {
    }

    public TemporaryResumeFileStore(string stagingRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingRootPath);
        this.stagingRootPath = stagingRootPath;
    }

    public async Task<StagedResumeFile> StageAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        var originalFileName = ExtractFileName(file.FileName);
        var extension = Path.GetExtension(originalFileName);

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ResumeImportValidationException("A resume file is required.");
        }

        if (file.Length <= 0)
        {
            throw new ResumeImportValidationException("The uploaded resume file is empty.");
        }

        if (!SupportedExtensions.Contains(extension))
        {
            throw new ResumeImportValidationException("Only PDF and DOCX resume files are supported.");
        }

        Directory.CreateDirectory(stagingRootPath);

        var storedFilePath = Path.Combine(
            stagingRootPath,
            $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}");

        try
        {
            await using var targetStream = new FileStream(
                storedFilePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await using var sourceStream = file.OpenReadStream();
            await sourceStream.CopyToAsync(targetStream, cancellationToken);
        }
        catch
        {
            if (File.Exists(storedFilePath))
            {
                File.Delete(storedFilePath);
            }

            throw;
        }

        return new StagedResumeFile(
            storedFilePath,
            originalFileName,
            file.ContentType ?? string.Empty,
            file.Length);
    }

    private static string ExtractFileName(string fileName)
    {
        var lastSeparatorIndex = Math.Max(fileName.LastIndexOf('/'), fileName.LastIndexOf('\\'));
        return lastSeparatorIndex >= 0
            ? fileName[(lastSeparatorIndex + 1)..]
            : fileName;
    }
}
