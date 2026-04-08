using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectPortfolio2026.Server.Contracts.Admin;
using ProjectPortfolio2026.Server.Domain.Identity;

namespace ProjectPortfolio2026.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<AdminSummaryResponse>(StatusCodes.Status200OK)]
    public ActionResult<AdminSummaryResponse> GetAsync()
    {
        return Ok(new AdminSummaryResponse
        {
            Message = "Admin API is available.",
            UserName = User.Identity?.Name ?? string.Empty
        });
    }
}
