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
                SourceUrl = "https://8.8.8.8/resume.pdf",
                DisplayLabel = "Public Resume"
            }
        };
        var service = new ResumeImportService(store, parser, repository, CreateHttpClient(HttpStatusCode.NotFound));

        Assert.That(async () => await service.ParseConfiguredSourceAsync(), Throws.TypeOf<ResumeImportValidationException>());
    }

    [Test]
    public void ParseConfiguredSourceAsync_ThrowsWhenConfiguredSourceTypeIsEmbed()
    {
        var store = new TemporaryResumeFileStore(tempRootPath);
        var parser = new TrackingResumeParserService();
        var repository = new StubResumeConfigurationRepository
        {
            Configuration = new ResumeConfiguration
            {
                SourceType = ResumeSourceTypes.Embed,
                SourceUrl = "https://8.8.8.8/embed/resume",
                DisplayLabel = "Public Resume"
            }
        };
        var httpClient = CreateHttpClient(out var handler);
        var service = new ResumeImportService(store, parser, repository, httpClient);

        var exception = Assert.ThrowsAsync<ResumeImportValidationException>(async () => await service.ParseConfiguredSourceAsync());

        Assert.Multiple(() =>
        {
            Assert.That(exception?.Message, Is.EqualTo("Only hosted file resume sources can be parsed from the configured source workflow."));
            Assert.That(handler.RequestCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void ParseConfiguredSourceAsync_WrapsNetworkFailuresAsValidationErrors()
    {
        var store = new TemporaryResumeFileStore(tempRootPath);
        var parser = new TrackingResumeParserService();
        var repository = new StubResumeConfigurationRepository
        {
            Configuration = new ResumeConfiguration
            {
                SourceType = ResumeSourceTypes.HostedFile,
                SourceUrl = "https://8.8.8.8/resume.pdf",
                DisplayLabel = "Public Resume"
            }
        };
        var httpClient = CreateHttpClient(new StubHttpResponse
        {
            ExceptionToThrow = new HttpRequestException("Host unreachable.")
        });
        var service = new ResumeImportService(store, parser, repository, httpClient);

        var exception = Assert.ThrowsAsync<ResumeImportValidationException>(async () => await service.ParseConfiguredSourceAsync());

        Assert.Multiple(() =>
        {
            Assert.That(exception?.Message, Is.EqualTo("Unable to download the configured resume source."));
            Assert.That(exception?.InnerException, Is.TypeOf<HttpRequestException>());
        });
    }

    [Test]
    public void ParseConfiguredSourceAsync_RejectsPrivateConfiguredSourceHostsBeforeDownload()
    {
        var store = new TemporaryResumeFileStore(tempRootPath);
        var parser = new TrackingResumeParserService();
        var repository = new StubResumeConfigurationRepository
        {
            Configuration = new ResumeConfiguration
            {
                SourceType = ResumeSourceTypes.HostedFile,
                SourceUrl = "https://127.0.0.1/resume.pdf",
                DisplayLabel = "Public Resume"
            }
        };
        var httpClient = CreateHttpClient(out var handler);
        var service = new ResumeImportService(store, parser, repository, httpClient);

        var exception = Assert.ThrowsAsync<ResumeImportValidationException>(async () => await service.ParseConfiguredSourceAsync());

        Assert.Multiple(() =>
        {
            Assert.That(exception?.Message, Is.EqualTo("Configured resume source URLs must resolve to a public host."));
            Assert.That(handler.RequestCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void ParseConfiguredSourceAsync_RejectsRedirectsToPrivateHosts()
    {
        var store = new TemporaryResumeFileStore(tempRootPath);
        var parser = new TrackingResumeParserService();
        var repository = new StubResumeConfigurationRepository
        {
            Configuration = new ResumeConfiguration
            {
                SourceType = ResumeSourceTypes.HostedFile,
                SourceUrl = "https://8.8.8.8/resume.pdf",
                DisplayLabel = "Public Resume"
            }
        };
        var httpClient = CreateHttpClient(out var handler,
            new StubHttpResponse
            {
                StatusCode = HttpStatusCode.Redirect,
                RedirectLocation = new Uri("https://127.0.0.1/internal.pdf")
            });
        var service = new ResumeImportService(store, parser, repository, httpClient);

        var exception = Assert.ThrowsAsync<ResumeImportValidationException>(async () => await service.ParseConfiguredSourceAsync());

        Assert.Multiple(() =>
        {
            Assert.That(exception?.Message, Is.EqualTo("Configured resume source URLs must resolve to a public host."));
            Assert.That(handler.RequestCount, Is.EqualTo(1));
        });
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
                SourceUrl = "https://8.8.8.8/resume.pdf",
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

    [Test]
    public async Task ParseConfiguredSourceAsync_InfersSupportedFileExtensionFromContentType()
    {
        var store = new TemporaryResumeFileStore(tempRootPath);
        var parser = new TrackingResumeParserService();
        var repository = new StubResumeConfigurationRepository
        {
            Configuration = new ResumeConfiguration
            {
                SourceType = ResumeSourceTypes.HostedFile,
                SourceUrl = "https://8.8.8.8/download/resume",
                DisplayLabel = "Public Resume"
            }
        };
        var sourceBytes = new byte[] { 21, 22, 23 };
        var service = new ResumeImportService(
            store,
            parser,
            repository,
            CreateHttpClient(content: sourceBytes, contentType: "application/pdf"));

        var result = await service.ParseConfiguredSourceAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.SourceFileName, Is.EqualTo("resume.pdf"));
            Assert.That(parser.CapturedFileName, Is.EqualTo("resume.pdf"));
            Assert.That(parser.CapturedBytes, Is.EqualTo(sourceBytes));
        });
    }

    [Test]
    public async Task ParseConfiguredSourceAsync_UsesContentDispositionFileNameStarWhenPresent()
    {
        var store = new TemporaryResumeFileStore(tempRootPath);
        var parser = new TrackingResumeParserService();
        var repository = new StubResumeConfigurationRepository
        {
            Configuration = new ResumeConfiguration
            {
                SourceType = ResumeSourceTypes.HostedFile,
                SourceUrl = "https://8.8.8.8/download/resume",
                DisplayLabel = "Public Resume"
            }
        };
        var sourceBytes = new byte[] { 31, 32, 33 };
        var service = new ResumeImportService(
            store,
            parser,
            repository,
            CreateHttpClient(content: sourceBytes, fileNameStar: "UTF-8''resume%20master.docx", contentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document"));

        var result = await service.ParseConfiguredSourceAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.SourceFileName, Is.EqualTo("resume master.docx"));
            Assert.That(parser.CapturedFileName, Is.EqualTo("resume master.docx"));
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

    private static HttpClient CreateHttpClient(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        byte[]? content = null,
        string? fileName = null,
        string? fileNameStar = null,
        string? contentType = null)
    {
        return CreateHttpClient(new StubHttpResponse
        {
            StatusCode = statusCode,
            Content = content ?? Array.Empty<byte>(),
            FileName = fileName,
            FileNameStar = fileNameStar,
            ContentType = contentType
        });
    }

    private static HttpClient CreateHttpClient(params StubHttpResponse[] responses)
    {
        var handler = new StubHttpMessageHandler(responses);
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.com/")
        };
    }

    private static HttpClient CreateHttpClient(out StubHttpMessageHandler handler, params StubHttpResponse[] responses)
    {
        handler = new StubHttpMessageHandler(responses);
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.com/")
        };
    }

    private sealed class StubHttpResponse
    {
        public byte[] Content { get; init; } = Array.Empty<byte>();

        public string? ContentType { get; init; }

        public Exception? ExceptionToThrow { get; init; }

        public string? FileName { get; init; }

        public string? FileNameStar { get; init; }

        public Uri? RedirectLocation { get; init; }

        public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<StubHttpResponse> responses;

        public int RequestCount { get; private set; }

        public StubHttpMessageHandler(params StubHttpResponse[] responses)
        {
            this.responses = new Queue<StubHttpResponse>(responses.Length == 0
                ? [new StubHttpResponse()]
                : responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount += 1;

            if (responses.Count == 0)
            {
                throw new InvalidOperationException("No stub HTTP response was configured.");
            }

            var nextResponse = responses.Dequeue();
            if (nextResponse.ExceptionToThrow is not null)
            {
                throw nextResponse.ExceptionToThrow;
            }

            var response = new HttpResponseMessage(nextResponse.StatusCode)
            {
                Content = new ByteArrayContent(nextResponse.Content),
                RequestMessage = request
            };

            if (!string.IsNullOrWhiteSpace(nextResponse.ContentType))
            {
                response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(nextResponse.ContentType);
            }

            if (!string.IsNullOrWhiteSpace(nextResponse.FileName) || !string.IsNullOrWhiteSpace(nextResponse.FileNameStar))
            {
                var contentDisposition = new ContentDispositionHeaderValue("attachment");
                if (!string.IsNullOrWhiteSpace(nextResponse.FileName))
                {
                    contentDisposition.FileName = nextResponse.FileName;
                }

                if (!string.IsNullOrWhiteSpace(nextResponse.FileNameStar))
                {
                    contentDisposition.FileNameStar = nextResponse.FileNameStar;
                }

                response.Content.Headers.ContentDisposition = contentDisposition;
            }

            if (nextResponse.RedirectLocation is not null)
            {
                response.Headers.Location = nextResponse.RedirectLocation;
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
