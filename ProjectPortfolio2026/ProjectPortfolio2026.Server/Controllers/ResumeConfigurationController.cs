using Microsoft.AspNetCore.Mvc;
using ProjectPortfolio2026.Server.Contracts;
using ProjectPortfolio2026.Server.Contracts.Resume;
using ProjectPortfolio2026.Server.Infrastructure.RequestTracking;
using ProjectPortfolio2026.Server.Mappers;
using ProjectPortfolio2026.Server.Repositories;
using ProjectPortfolio2026.Server.Domain.Portfolio;

namespace ProjectPortfolio2026.Server.Controllers;

[ApiController]
[Route("api/resume-configuration")]
public sealed class ResumeConfigurationController(IResumeConfigurationRepository resumeConfigurationRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ResumeConfigurationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResumeConfigurationResponse>> GetAsync(CancellationToken cancellationToken)
    {
        var configuration = await resumeConfigurationRepository.GetAsync(cancellationToken);
        if (!ResumeConfigurationRules.HasCompletePublicConfiguration(configuration))
        {
            return NotFound("The requested resume configuration could not be found.");
        }

        var requestId = HttpContext.Items[RequestIdContext.ItemKey] as string;
        return Ok(configuration!.ToResponse(requestId));
    }
}
