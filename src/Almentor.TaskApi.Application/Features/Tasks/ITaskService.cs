using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Application.Features.Tasks.Dtos;
using Almentor.TaskApi.Application.Features.Tasks.Querying;

namespace Almentor.TaskApi.Application.Features.Tasks;

public interface ITaskService
{
    Task<TaskResponse> CreateAsync(Guid projectId, CreateTaskRequest request, CancellationToken ct);

    /// <summary>Lists tasks. projectId null = across all projects; set = scoped (404 if that project is missing).</summary>
    Task<PagedResult<TaskResponse>> GetPagedAsync(Guid? projectId, TaskQueryParameters query, CancellationToken ct);

    Task<TaskResponse> GetByIdAsync(Guid id, CancellationToken ct);

    Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct);

    Task DeleteAsync(Guid id, CancellationToken ct);
}
