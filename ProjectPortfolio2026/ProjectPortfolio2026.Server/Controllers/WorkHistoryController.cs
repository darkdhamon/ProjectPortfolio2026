using Microsoft.AspNetCore.Mvc;

using ProjectPortfolio2026.Server.Contracts.WorkHistory;
using ProjectPortfolio2026.Server.Infrastructure.RequestTracking;
using ProjectPortfolio2026.Server.Mappers;
using ProjectPortfolio2026.Server.Repositories;

namespace ProjectPortfolio2026.Server.Controllers;

[ApiController]
[Route("api/work-history")]
public sealed class WorkHistoryController(IEmployerRepository employerRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<WorkHistoryResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkHistoryResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var employers = await employerRepository.ListPublishedAsync(cancellationToken);
        var requestId = HttpContext.Items[RequestIdContext.ItemKey] as string;

        return Ok(new WorkHistoryResponse
        {
            RequestId = requestId,
            Items = employers
                .Select(employer => employer.ToResponse())
                .ToList()
        });
    }
}
