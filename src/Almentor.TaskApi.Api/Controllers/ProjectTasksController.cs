using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Application.Features.Tasks;
using Almentor.TaskApi.Application.Features.Tasks.Dtos;
using Almentor.TaskApi.Application.Features.Tasks.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Almentor.TaskApi.Api.Controllers;

// Task operations that are scoped to a parent project.
[ApiController]
[Route("api/projects/{projectId:guid}/tasks")]
[Authorize]
public class ProjectTasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public ProjectTasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> Create(
        Guid projectId, [FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var task = await _taskService.CreateAsync(projectId, request, ct);

        return CreatedAtAction(
            actionName: nameof(TasksController.GetById),
            controllerName: "Tasks",
            routeValues: new { id = task.Id },
            value: ApiResponse<TaskResponse>.Ok(task));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TaskResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TaskResponse>>>> GetForProject(
        Guid projectId, [FromQuery] TaskQueryParameters query, CancellationToken ct)
    {
        var page = await _taskService.GetPagedAsync(projectId, query, ct);
        return Ok(page.ToApiResponse());
    }
}
