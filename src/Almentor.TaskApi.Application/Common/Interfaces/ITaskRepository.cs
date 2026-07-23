using Almentor.TaskApi.Domain.Entities;

namespace Almentor.TaskApi.Application.Common.Interfaces;

public interface ITaskRepository
{
    // Fetches a task with its owning Project attached, so ProjectName is available without a second query
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct);

    Task AddAsync(TaskItem task, CancellationToken ct);

    void Update(TaskItem task);

    void Remove(TaskItem task);

    Task SaveChangesAsync(CancellationToken ct);
}
