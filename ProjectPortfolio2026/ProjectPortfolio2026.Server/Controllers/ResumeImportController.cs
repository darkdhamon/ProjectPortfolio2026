using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectPortfolio2026.Server.Contracts;
using ProjectPortfolio2026.Server.Contracts.Admin.ResumeImport;
using ProjectPortfolio2026.Server.Domain.Identity;
using ProjectPortfolio2026.Server.Mappers;
using ProjectPortfolio2026.Server.Services.Interfaces;
using ProjectPortfolio2026.Server.Services.ServiceModels;

namespace ProjectPortfolio2026.Server.Controllers;

[ApiController]
[Route("api/admin/resume-import")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class ResumeImportController(IResumeImportService resumeImportService) : ControllerBase
{
    [HttpPost("parse")]
    [ValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<ResumeImportParseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResumeImportParseResponse>> ParseAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return BadRequest(new ApiErrorResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
                ErrorCode = "resume_file_required",
                Message = "A resume file is required."
            });
        }

        try
        {
            var result = await resumeImportService.ParseAsync(file, cancellationToken);
            return Ok(ResumeImportContractMapper.Map(result));
        }
        catch (ResumeImportValidationException exception)
        {
            return BadRequest(new ApiErrorResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
                ErrorCode = "resume_file_invalid",
                Message = exception.Message
            });
        }
    }
}
