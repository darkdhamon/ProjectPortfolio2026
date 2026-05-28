using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Services.Implementations;
using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class TemporaryResumeFileStoreTests
{
    private string tempRootPath = null!;

    [SetUp]
    public void SetUp()
    {
        tempRootPath = Path.Combine(Path.GetTempPath(), "ProjectPortfolio2026.Tests", Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(tempRootPath))
        {
            Directory.Delete(tempRootPath, recursive: true);
        }
    }

    [Test]
    public async Task StageAsync_CopiesSupportedPdfAndDeletesItOnDispose()
    {
        var store = new TemporaryResumeFileStore(tempRootPath);
        var file = CreateFormFile("resume.pdf", "application/pdf", [1, 2, 3, 4]);

        var stagedFile = await store.StageAsync(file);

        Assert.That(File.Exists(stagedFile.StoredFilePath), Is.True);
        Assert.That(await File.ReadAllBytesAsync(stagedFile.StoredFilePath), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));

        await stagedFile.DisposeAsync();

        Assert.That(File.Exists(stagedFile.StoredFilePath), Is.False);
    }

    [TestCase("temp/Resume.DOCX")]
    [TestCase(@"temp\Resume.DOCX")]
    public async Task StageAsync_StripsPathSegmentsAndNormalizesStoredExtension(string uploadedFileName)
    {
        var store = new TemporaryResumeFileStore(tempRootPath);
        var file = CreateFormFile(uploadedFileName, null, [1, 2, 3]);

        var stagedFile = await store.StageAsync(file);

        Assert.Multiple(() =>
        {
            Assert.That(stagedFile.OriginalFileName, Is.EqualTo("Resume.DOCX"));
            Assert.That(stagedFile.StoredFilePath, Does.EndWith(".docx"));
            Assert.That(stagedFile.ContentType, Is.Empty);
        });

        await stagedFile.DisposeAsync();
    }

    [Test]
    public void StageAsync_RejectsUnsupportedFileExtension()
    {
        var store = new TemporaryResumeFileStore(tempRootPath);
        var file = CreateFormFile("resume.txt", "text/plain", [1, 2, 3]);

        var exception = Assert.ThrowsAsync<ResumeImportValidationException>(async () => await store.StageAsync(file));

        Assert.That(exception?.Message, Is.EqualTo("Only PDF and DOCX resume files are supported."));
    }

    [Test]
    public void StageAsync_RejectsMissingFileName()
    {
        var store = new TemporaryResumeFileStore(tempRootPath);
        var file = CreateFormFile(string.Empty, "application/pdf", [1, 2, 3]);

        var exception = Assert.ThrowsAsync<ResumeImportValidationException>(async () => await store.StageAsync(file));

        Assert.That(exception?.Message, Is.EqualTo("A resume file is required."));
    }

    [Test]
    public void StageAsync_RejectsEmptyFiles()
    {
        var store = new TemporaryResumeFileStore(tempRootPath);
        var file = CreateFormFile("resume.pdf", "application/pdf", []);

        var exception = Assert.ThrowsAsync<ResumeImportValidationException>(async () => await store.StageAsync(file));

        Assert.That(exception?.Message, Is.EqualTo("The uploaded resume file is empty."));
    }

    private static FormFile CreateFormFile(string fileName, string? contentType, byte[] content)
    {
        var stream = new MemoryStream(content);
        var formFile = new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType ?? string.Empty
        };

        return formFile;
    }
}
