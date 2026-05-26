namespace ProjectPortfolio2026.Server.Services.ServiceModels;

public sealed class StagedResumeFile(
    string storedFilePath,
    string originalFileName,
    string contentType,
    long fileLength) : IAsyncDisposable
{
    public string StoredFilePath { get; } = storedFilePath;

    public string OriginalFileName { get; } = originalFileName;

    public string ContentType { get; } = contentType;

    public long FileLength { get; } = fileLength;

    public ValueTask DisposeAsync()
    {
        if (File.Exists(StoredFilePath))
        {
            File.Delete(StoredFilePath);
        }

        return ValueTask.CompletedTask;
    }
}
