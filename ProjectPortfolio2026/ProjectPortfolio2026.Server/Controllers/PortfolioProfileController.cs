using Microsoft.AspNetCore.Mvc;
using ProjectPortfolio2026.Server.Contracts;
using ProjectPortfolio2026.Server.Contracts.Portfolio;
using ProjectPortfolio2026.Server.Infrastructure.RequestTracking;
using ProjectPortfolio2026.Server.Mappers;
using ProjectPortfolio2026.Server.Repositories;

namespace ProjectPortfolio2026.Server.Controllers;

[ApiController]
[Route("api/portfolio-profile")]
public sealed class PortfolioProfileController(IPortfolioProfileRepository portfolioProfileRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PortfolioProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PortfolioProfileResponse>> GetAsync(CancellationToken cancellationToken)
    {
        var profile = await portfolioProfileRepository.GetPublicAsync(cancellationToken);
        if (profile is null)
        {
            return NotFound(new ApiErrorResponse { Message = "The requested portfolio profile could not be found." });
        }

        var requestId = HttpContext.Items[RequestIdContext.ItemKey] as string;
        return Ok(profile.ToResponse(requestId));
    }
}
