using Almentor.TaskApi.Application.Features.Tasks.Dtos;

namespace Almentor.TaskApi.Application.Features.Tasks;

public interface ITaskService
{
    Task<TaskResponse> CreateAsync(Guid projectId, CreateTaskRequest request, CancellationToken ct);

    Task<TaskResponse> GetByIdAsync(Guid id, CancellationToken ct);

    Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct);

    Task DeleteAsync(Guid id, CancellationToken ct);
}
