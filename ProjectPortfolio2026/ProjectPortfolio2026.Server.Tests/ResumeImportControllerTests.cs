using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using ProjectPortfolio2026.Server.Contracts;
using ProjectPortfolio2026.Server.Contracts.Admin.ResumeImport;
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
            Result = new ResumeImportCandidateResult
            {
                Person = new ParsedPerson
                {
                    FullName = "Taylor Jordan"
                },
                SourceFileName = "resume.pdf",
                ParserName = "StubParser",
                GlobalSkills = ["C#"],
                CandidateWorkHistory =
                [
                    new ResumeImportEmployerCandidate
                    {
                        CandidateId = "employer-001",
                        EmployerName = "Northwind Health",
                        JobRoles =
                        [
                            new ResumeImportJobRoleCandidate
                            {
                                CandidateId = "role-001",
                                JobTitle = "Staff Engineer",
                                DescriptionMarkdown = "Led modernization.",
                                RawFields = new Dictionary<string, string?>(StringComparer.Ordinal)
                                {
                                    ["source-section"] = "experience"
                                }
                            }
                        ]
                    }
                ]
            }
        });

        var result = await controller.ParseAsync(CreateFormFile("resume.pdf"), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var response = (result.Result as OkObjectResult)?.Value as ResumeImportParseResponse;

        Assert.Multiple(() =>
        {
            Assert.That(response, Is.Not.Null);
            Assert.That(response?.Person?.FullName, Is.EqualTo("Taylor Jordan"));
            Assert.That(response?.SourceFileName, Is.EqualTo("resume.pdf"));
            Assert.That(response?.ParserName, Is.EqualTo("StubParser"));
            Assert.That(response?.GlobalSkills, Is.EqualTo(new[] { "C#" }));
            Assert.That(response?.CandidateWorkHistory, Has.Count.EqualTo(1));
            Assert.That(response?.CandidateWorkHistory[0].CandidateId, Is.EqualTo("employer-001"));
            Assert.That(response?.CandidateWorkHistory[0].EmployerName, Is.EqualTo("Northwind Health"));
            Assert.That(response?.CandidateWorkHistory[0].JobRoles, Has.Count.EqualTo(1));
            Assert.That(response?.CandidateWorkHistory[0].JobRoles[0].CandidateId, Is.EqualTo("role-001"));
            Assert.That(response?.CandidateWorkHistory[0].JobRoles[0].JobTitle, Is.EqualTo("Staff Engineer"));
            Assert.That(response?.CandidateWorkHistory[0].JobRoles[0].DescriptionMarkdown, Is.EqualTo("Led modernization."));
            Assert.That(response?.CandidateWorkHistory[0].JobRoles[0].RawFields["source-section"], Is.EqualTo("experience"));
        });
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

        public ResumeImportCandidateResult Result { get; set; } = new();

        public Task<ResumeImportCandidateResult> ParseAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(Result);
        }
    }
}
