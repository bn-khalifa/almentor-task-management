using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Application.Features.Tasks;
using Almentor.TaskApi.Application.Features.Tasks.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Almentor.TaskApi.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var task = await _taskService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<TaskResponse>.Ok(task));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> Update(
        Guid id, [FromBody] UpdateTaskRequest request, CancellationToken ct)
    {
        var task = await _taskService.UpdateAsync(id, request, ct);
        return Ok(ApiResponse<TaskResponse>.Ok(task));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _taskService.DeleteAsync(id, ct);
        return NoContent();
    }
}
