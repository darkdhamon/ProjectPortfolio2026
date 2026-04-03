using Microsoft.AspNetCore.Mvc;
using ProjectPortfolio2026.Server.Contracts;
using ProjectPortfolio2026.Server.Contracts.Projects;
using ProjectPortfolio2026.Server.Infrastructure.RequestTracking;
using ProjectPortfolio2026.Server.Mappers;
using ProjectPortfolio2026.Server.Repositories;

namespace ProjectPortfolio2026.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProjectsController(IProjectRepository projectRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ProjectListResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectListResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var projects = await projectRepository.ListAsync(cancellationToken);
        var requestId = HttpContext.Items[RequestIdContext.ItemKey] as string;
        var items = projects.Select(project => project.ToResponse(requestId)).ToList();

        return Ok(new ProjectListResponse
        {
            RequestId = requestId,
            Items = items
        });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProjectResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponse>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(id, cancellationToken);
        var requestId = HttpContext.Items[RequestIdContext.ItemKey] as string;

        return project is null ? NotFound() : Ok(project.ToResponse(requestId));
    }

    [HttpPost]
    [ProducesResponseType<ProjectResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProjectResponse>> CreateAsync(
        [FromBody] ProjectRequest request,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.Items[RequestIdContext.ItemKey] as string;
        var savedProject = await projectRepository.AddAsync(request.ToDomain(), cancellationToken);
        var response = savedProject.ToResponse(requestId);

        return CreatedAtAction(nameof(GetByIdAsync), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ProjectResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponse>> UpdateAsync(
        int id,
        [FromBody] ProjectRequest request,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.Items[RequestIdContext.ItemKey] as string;
        var existingProject = await projectRepository.GetByIdAsync(id, cancellationToken);
        if (existingProject is null)
        {
            return NotFound();
        }

        request.ApplyTo(existingProject);
        var updatedProject = await projectRepository.UpdateAsync(existingProject, cancellationToken);

        return Ok(updatedProject!.ToResponse(requestId));
    }
}
