using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectPortfolio2026.Server.Contracts;
using ProjectPortfolio2026.Server.Contracts.Projects;
using ProjectPortfolio2026.Server.Domain.Identity;
using ProjectPortfolio2026.Server.Infrastructure.RequestTracking;
using ProjectPortfolio2026.Server.Mappers;
using ProjectPortfolio2026.Server.Repositories;

namespace ProjectPortfolio2026.Server.Controllers;

[ApiController]
[Route("api/admin/projects")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminProjectsController(IProjectRepository projectRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProjectSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProjectSummaryResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        var projects = await projectRepository.ListAllAsync(cancellationToken);
        var requestId = HttpContext.Items[RequestIdContext.ItemKey] as string;
        return Ok(projects.Select(project => project.ToResponse(requestId)).ToList());
    }

    [HttpPut("{id:int}/featured-state")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<ProjectResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponse>> SetFeaturedStateAsync(
        int id,
        [FromBody] ProjectFeaturedStateRequest request,
        CancellationToken cancellationToken)
    {
        var updatedProject = await projectRepository.UpdateFeaturedStateAsync(id, request.IsFeatured, cancellationToken);
        if (updatedProject is null)
        {
            return NotFound(new ApiErrorResponse
            {
                Message = "The requested project could not be found."
            });
        }

        return Ok(updatedProject.ToResponse());
    }

    [HttpPut("featured-order")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetFeaturedOrderAsync(
        [FromBody] ProjectFeaturedOrderUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var isUpdated = await projectRepository.ReorderFeaturedProjectsAsync(request.ProjectIds, cancellationToken);

        if (!isUpdated)
        {
            return NotFound(new ApiErrorResponse
            {
                Message = "The requested project order could not be applied because one or more projects were not found."
            });
        }

        return NoContent();
    }
}
