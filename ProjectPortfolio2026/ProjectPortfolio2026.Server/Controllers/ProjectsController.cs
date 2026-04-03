using Microsoft.AspNetCore.Mvc;
using ProjectPortfolio2026.Server.Contracts;
using ProjectPortfolio2026.Server.Contracts.Projects;
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
        var requestId = ResolveRequestId();
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
        var requestId = ResolveRequestId();

        return project is null
            ? NotFound(CreateNotFoundError("project_not_found", $"Project {id} was not found.", requestId))
            : Ok(project.ToResponse(requestId));
    }

    [HttpPost]
    [ProducesResponseType<ProjectResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProjectResponse>> CreateAsync(
        [FromBody] ProjectRequest request,
        CancellationToken cancellationToken)
    {
        var requestId = ResolveRequestId(request);
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
        var requestId = ResolveRequestId(request);
        var existingProject = await projectRepository.GetByIdAsync(id, cancellationToken);
        if (existingProject is null)
        {
            return NotFound(CreateNotFoundError("project_not_found", $"Project {id} was not found.", requestId));
        }

        request.ApplyTo(existingProject);
        var updatedProject = await projectRepository.UpdateAsync(existingProject, cancellationToken);

        return Ok(updatedProject!.ToResponse(requestId));
    }

    private string? ResolveRequestId(ApiRequestDto? request = null)
    {
        if (!string.IsNullOrWhiteSpace(request?.RequestId))
        {
            return request.RequestId.Trim();
        }

        var queryRequestId = Request.Query["requestId"].FirstOrDefault()
            ?? Request.Query["RequestId"].FirstOrDefault()
            ?? Request.Query["x-request-id"].FirstOrDefault()
            ?? Request.Query["X-Request-Id"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(queryRequestId))
        {
            return queryRequestId.Trim();
        }

        var headerRequestId = Request.Headers["X-Request-Id"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(headerRequestId) ? null : headerRequestId.Trim();
    }

    private static ApiErrorResponse CreateNotFoundError(string errorCode, string message, string? requestId)
    {
        return new ApiErrorResponse
        {
            RequestId = requestId,
            ErrorCode = errorCode,
            Message = message
        };
    }
}
