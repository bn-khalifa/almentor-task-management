using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Domain.Entities;

namespace Almentor.TaskApi.Application.Common.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken ct);

    // Delete-path load: tracked and with Tasks included, so soft-delete can
    // cascade to the project's tasks in memory. Reads use GetByIdAsync instead.
    Task<Project?> GetByIdWithTasksAsync(Guid id, CancellationToken ct);

    Task<PagedResult<Project>> GetPagedAsync(PaginationParams pagination, CancellationToken ct);

    // Case-insensitive existence check used for the app-layer duplicate-name pre-check
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct);

    Task AddAsync(Project project, CancellationToken ct);

    void Update(Project project);

    void Remove(Project project);

    // Commits the unit of work. Throws DuplicateNameException if the DB's unique index rejects the write
    Task SaveChangesAsync(CancellationToken ct);
}
