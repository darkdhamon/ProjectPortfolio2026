using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Contracts;
using ProjectPortfolio2026.Server.Controllers;
using ProjectPortfolio2026.Server.Services.Interfaces;
using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Tests;

[TestFixture]
public sealed class ResumeImportControllerTests
{
    [Test]
    public async Task ParseAsync_ReturnsBadRequestWhenFileIsMissing()
    {
        var controller = new ResumeImportController(new StubResumeImportService());

        var result = await controller.ParseAsync(null, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        var error = (result.Result as BadRequestObjectResult)?.Value as ApiErrorResponse;
        Assert.That(error?.Message, Is.EqualTo("A resume file is required."));
    }

    [Test]
    public async Task ParseAsync_ReturnsBadRequestWhenUploadValidationFails()
    {
        var controller = new ResumeImportController(new StubResumeImportService
        {
            ExceptionToThrow = new ResumeImportValidationException("Only PDF and DOCX resume files are supported.")
        });

        var result = await controller.ParseAsync(CreateFormFile("resume.txt"), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        var error = (result.Result as BadRequestObjectResult)?.Value as ApiErrorResponse;
        Assert.That(error?.Message, Is.EqualTo("Only PDF and DOCX resume files are supported."));
    }

    [Test]
    public async Task ParseAsync_ReturnsParsedPayloadWhenUploadSucceeds()
    {
        var controller = new ResumeImportController(new StubResumeImportService
        {
            Result = new ResumeImportParseResult
            {
                SourceFileName = "resume.pdf",
                ParserName = "StubParser",
                GlobalSkills = ["C#"]
            }
        });

        var result = await controller.ParseAsync(CreateFormFile("resume.pdf"), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var response = (result.Result as OkObjectResult)?.Value;
        Assert.That(response, Is.Not.Null);
    }

    private static FormFile CreateFormFile(string fileName)
    {
        return new FormFile(new MemoryStream([1, 2, 3]), 0, 3, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };
    }

    private sealed class StubResumeImportService : IResumeImportService
    {
        public ResumeImportValidationException? ExceptionToThrow { get; set; }

        public ResumeImportParseResult Result { get; set; } = new();

        public Task<ResumeImportParseResult> ParseAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(Result);
        }
    }
}
