using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Domain.Entities;

namespace Almentor.TaskApi.Application.Common.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken ct);

    // Delete-path load: tracked and with Tasks included, so soft-delete can
    // cascade to the project's tasks in memory. Reads use GetByIdAsync instead.
    Task<Project?> GetByIdWithTasksAsync(Guid id, CancellationToken ct);

    // Scoped to the owner — a user only ever lists their own projects.
    Task<PagedResult<Project>> GetPagedAsync(Guid ownerId, PaginationParams pagination, CancellationToken ct);

    // Case-insensitive duplicate-name pre-check, scoped per owner (names are
    // unique within an owner, not globally).
    Task<bool> ExistsByNameAsync(Guid ownerId, string name, Guid? excludeId, CancellationToken ct);

    Task AddAsync(Project project, CancellationToken ct);

    void Update(Project project);

    void Remove(Project project);

    // Commits the unit of work. Throws DuplicateNameException if the DB's unique index rejects the write
    Task SaveChangesAsync(CancellationToken ct);
}
