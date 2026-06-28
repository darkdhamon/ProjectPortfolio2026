using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Domain.Portfolio;
using ProjectPortfolio2026.Server.Repositories;
using ProjectPortfolio2026.Server.Services.Implementations;
using ProjectPortfolio2026.Server.Services.Interfaces;
using ProjectPortfolio2026.Server.Services.ServiceModels;
using System.Net;
using System.Net.Http.Headers;

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
        var service = new ResumeImportService(store, parser, new StubResumeConfigurationRepository(), CreateHttpClient());
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
        var service = new ResumeImportService(store, parser, new StubResumeConfigurationRepository(), CreateHttpClient());
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
        var service = new ResumeImportService(store, parser, new StubResumeConfigurationRepository(), CreateHttpClient());
        var file = CreateFormFile("resume.pdf", "application/pdf", [1, 2, 3]);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await service.ParseAsync(file));
        Assert.That(Directory.Exists(tempRootPath), Is.True);
        Assert.That(Directory.EnumerateFiles(tempRootPath), Is.Empty);
    }

    [Test]
    public void ParseConfiguredSourceAsync_ThrowsWhenConfigurationIsIncomplete()
    {
        var store = new TemporaryResumeFileStore(tempRootPath);
        var parser = new TrackingResumeParserService();
        var service = new ResumeImportService(store, parser, new StubResumeConfigurationRepository(), CreateHttpClient());

        Assert.That(async () => await service.ParseConfiguredSourceAsync(), Throws.TypeOf<ResumeImportValidationException>());
    }

    [Test]
    public void ParseConfiguredSourceAsync_ThrowsWhenDownloadFails()
    {
        var store = new TemporaryResumeFileStore(tempRootPath);
        var parser = new TrackingResumeParserService();
        var repository = new StubResumeConfigurationRepository
        {
            Configuration = new ResumeConfiguration
            {
                SourceType = ResumeSourceTypes.HostedFile,
                SourceUrl = "https://cdn.example.dev/resume.pdf",
                DisplayLabel = "Public Resume"
            }
        };
        var service = new ResumeImportService(store, parser, repository, CreateHttpClient(HttpStatusCode.NotFound));

        Assert.That(async () => await service.ParseConfiguredSourceAsync(), Throws.TypeOf<ResumeImportValidationException>());
    }

    [Test]
    public async Task ParseConfiguredSourceAsync_DownloadsAndParsesConfiguredSource()
    {
        var store = new TemporaryResumeFileStore(tempRootPath);
        var parser = new TrackingResumeParserService();
        var repository = new StubResumeConfigurationRepository
        {
            Configuration = new ResumeConfiguration
            {
                SourceType = ResumeSourceTypes.HostedFile,
                SourceUrl = "https://cdn.example.dev/resume.pdf",
                DisplayLabel = "Public Resume"
            }
        };
        var sourceBytes = new byte[] { 11, 12, 13 };
        var service = new ResumeImportService(
            store,
            parser,
            repository,
            CreateHttpClient(content: sourceBytes, fileName: "resume.pdf"));

        var result = await service.ParseConfiguredSourceAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.SourceFileName, Is.EqualTo("resume.pdf"));
            Assert.That(parser.CapturedFileName, Is.EqualTo("resume.pdf"));
            Assert.That(parser.CapturedBytes, Is.EqualTo(sourceBytes));
        });
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

    private static HttpClient CreateHttpClient(HttpStatusCode statusCode = HttpStatusCode.OK, byte[]? content = null, string? fileName = null)
    {
        return new HttpClient(new StubHttpMessageHandler(statusCode, content ?? Array.Empty<byte>(), fileName))
        {
            BaseAddress = new Uri("https://example.com/")
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode statusCode;
        private readonly byte[] content;
        private readonly string? fileName;

        public StubHttpMessageHandler(HttpStatusCode statusCode, byte[] content, string? fileName)
        {
            this.statusCode = statusCode;
            this.content = content;
            this.fileName = fileName;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new ByteArrayContent(content)
            };

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = fileName
                };
            }

            return Task.FromResult(response);
        }
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

    private sealed class StubResumeConfigurationRepository : IResumeConfigurationRepository
    {
        public ResumeConfiguration? Configuration { get; set; }

        public Task<ResumeConfiguration?> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Configuration);
        }

        public Task<ResumeConfiguration> SaveAsync(ResumeConfiguration configuration, CancellationToken cancellationToken = default)
        {
            Configuration = configuration;
            return Task.FromResult(configuration);
        }
    }
}
