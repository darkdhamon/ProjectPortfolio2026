using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectPortfolio2026.Server.Contracts.Admin;
using ProjectPortfolio2026.Server.Contracts.Resume;
using ProjectPortfolio2026.Server.Domain.Identity;
using ProjectPortfolio2026.Server.Domain.Portfolio;
using ProjectPortfolio2026.Server.Infrastructure.RequestTracking;
using ProjectPortfolio2026.Server.Mappers;
using ProjectPortfolio2026.Server.Repositories;

namespace ProjectPortfolio2026.Server.Controllers;

[ApiController]
[Route("api/admin/resume-configuration")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminResumeConfigurationController(IResumeConfigurationRepository resumeConfigurationRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ResumeConfigurationResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResumeConfigurationResponse>> GetAsync(CancellationToken cancellationToken)
    {
        var requestId = HttpContext.Items[RequestIdContext.ItemKey] as string;
        var configuration = await resumeConfigurationRepository.GetAsync(cancellationToken)
            ?? new ResumeConfiguration();

        return Ok(configuration.ToResponse(requestId));
    }

    [HttpPut]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<ResumeConfigurationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResumeConfigurationResponse>> UpdateAsync(
        [FromBody] ResumeConfigurationUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ResumeConfigurationRules.Validate(request.SourceType, request.SourceUrl, request.DisplayLabel);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(validationErrors.ToDictionary(pair => pair.Key, pair => pair.Value)));
        }

        var savedConfiguration = await resumeConfigurationRepository.SaveAsync(
            new ResumeConfiguration
            {
                SourceType = ResumeSourceTypes.Normalize(request.SourceType),
                SourceUrl = ResumeConfigurationRules.NormalizeText(request.SourceUrl),
                DisplayLabel = ResumeConfigurationRules.NormalizeText(request.DisplayLabel),
                Summary = ResumeConfigurationRules.NormalizeText(request.Summary)
            },
            cancellationToken);

        var requestId = HttpContext.Items[RequestIdContext.ItemKey] as string;
        return Ok(savedConfiguration.ToResponse(requestId));
    }
}
