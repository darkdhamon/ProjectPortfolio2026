using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Services.Implementations;
using ProjectPortfolio2026.Server.Services.Interfaces;
using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class ResumeImportServiceTests
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
    public async Task ParseAsync_DeletesTemporaryUploadAfterSuccessfulParse()
    {
        var store = new TemporaryResumeFileStore(tempRootPath);
        var parser = new TrackingResumeParserService();
        var service = new ResumeImportService(store, parser);
        var file = CreateFormFile("resume.pdf", "application/pdf", [9, 8, 7]);

        var result = await service.ParseAsync(file);

        Assert.Multiple(() =>
        {
            Assert.That(result.SourceFileName, Is.EqualTo("resume.pdf"));
            Assert.That(parser.CapturedFileName, Is.EqualTo("resume.pdf"));
            Assert.That(parser.CapturedBytes, Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(Directory.Exists(tempRootPath), Is.True);
            Assert.That(Directory.EnumerateFiles(tempRootPath), Is.Empty);
        });
    }

    [Test]
    public async Task ParseAsync_PreservesParserSuppliedSourceFileName()
    {
        var store = new TemporaryResumeFileStore(tempRootPath);
        var parser = new TrackingResumeParserService
        {
            ResultFactory = () => new ResumeImportParseResult
            {
                SourceFileName = "normalized-resume.pdf",
                ParserName = "TrackingResumeParserService"
            }
        };
        var service = new ResumeImportService(store, parser);
        var file = CreateFormFile("resume.pdf", "application/pdf", [9, 8, 7]);

        var result = await service.ParseAsync(file);

        Assert.Multiple(() =>
        {
            Assert.That(result.SourceFileName, Is.EqualTo("normalized-resume.pdf"));
            Assert.That(parser.CapturedFileName, Is.EqualTo("resume.pdf"));
            Assert.That(Directory.Exists(tempRootPath), Is.True);
            Assert.That(Directory.EnumerateFiles(tempRootPath), Is.Empty);
        });
    }

    [Test]
    public void ParseAsync_DeletesTemporaryUploadWhenParserFails()
    {
        var store = new TemporaryResumeFileStore(tempRootPath);
        var parser = new ThrowingResumeParserService();
        var service = new ResumeImportService(store, parser);
        var file = CreateFormFile("resume.pdf", "application/pdf", [1, 2, 3]);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await service.ParseAsync(file));
        Assert.That(Directory.Exists(tempRootPath), Is.True);
        Assert.That(Directory.EnumerateFiles(tempRootPath), Is.Empty);
    }

    private static FormFile CreateFormFile(string fileName, string contentType, byte[] content)
    {
        var stream = new MemoryStream(content);
        var formFile = new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };

        return formFile;
    }

    private sealed class TrackingResumeParserService : IResumeParserService
    {
        public byte[] CapturedBytes { get; private set; } = [];

        public string? CapturedFileName { get; private set; }

        public Func<ResumeImportParseResult> ResultFactory { get; set; } = () => new ResumeImportParseResult
        {
            ParserName = "TrackingResumeParserService"
        };

        public async Task<ResumeImportParseResult> ParseAsync(
            Stream content,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            using var memoryStream = new MemoryStream();
            await content.CopyToAsync(memoryStream, cancellationToken);
            CapturedBytes = memoryStream.ToArray();
            CapturedFileName = fileName;

            return ResultFactory();
        }
    }

    private sealed class ThrowingResumeParserService : IResumeParserService
    {
        public Task<ResumeImportParseResult> ParseAsync(
            Stream content,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Parser failed.");
        }
    }
}
