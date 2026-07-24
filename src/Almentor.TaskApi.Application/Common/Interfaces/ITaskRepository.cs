using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Application.Features.Tasks.Querying;
using Almentor.TaskApi.Domain.Entities;

namespace Almentor.TaskApi.Application.Common.Interfaces;

public interface ITaskRepository
{
    // Fetches a task with its owning Project attached, so ProjectName is available without a second query
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct);

    // Filtered/sorted/paged task list. Includes each task's Project (single JOIN,
    // no N+1) so ProjectName is available on every row.
    Task<PagedResult<TaskItem>> GetPagedAsync(TaskListQuery query, CancellationToken ct);

    Task AddAsync(TaskItem task, CancellationToken ct);

    void Update(TaskItem task);

    void Remove(TaskItem task);

    Task SaveChangesAsync(CancellationToken ct);
}
