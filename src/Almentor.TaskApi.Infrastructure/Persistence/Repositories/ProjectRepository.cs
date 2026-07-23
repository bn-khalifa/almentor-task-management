using Almentor.TaskApi.Application.Common.Exceptions;
using Almentor.TaskApi.Application.Common.Interfaces;
using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Almentor.TaskApi.Infrastructure.Persistence.Repositories;

// The only place in the codebase that knows about DbContext/SQL Server for Project data.
public class ProjectRepository : IProjectRepository
{
    // SQL Server error numbers for a unique-index/constraint violation.
    private const int UniqueConstraintViolation = 2627;
    private const int UniqueIndexViolation = 2601;

    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken ct) =>
        // AsNoTracking: this path is read-only in every caller except Update/Delete,
        // which re-attach explicitly via Update()/Remove() below, so tracking here
        // by default would only add overhead to the common read case.
        _context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<PagedResult<Project>> GetPagedAsync(PaginationParams pagination, CancellationToken ct)
    {
        var query = _context.Projects.AsNoTracking().OrderBy(p => p.CreatedAt);

        // Two focused queries (count + page) rather than loading everything into memory to count it.
        var total = await query.CountAsync(ct);

        var items = await query
            .Skip(pagination.Offset)
            .Take(pagination.Limit)
            .ToListAsync(ct);

        return new PagedResult<Project>
        {
            Items = items,
            Total = total,
            Offset = pagination.Offset,
            Limit = pagination.Limit
        };
    }

    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct)
    {
        var query = _context.Projects.AsNoTracking()
            .Where(p => p.Name.ToLower() == name.ToLower());

        if (excludeId is not null)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return query.AnyAsync(ct);
    }

    public async Task AddAsync(Project project, CancellationToken ct)
    {
        project.Id = Guid.NewGuid();
        await _context.Projects.AddAsync(project, ct);
    }

    public void Update(Project project) => _context.Projects.Update(project);

    public void Remove(Project project) => _context.Projects.Remove(project);

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Race-safe backstop: two concurrent requests could both pass the app-layer ExistsByNameAsync check before either commits.
            // The DB's unique index is the real guarantee; 
            throw new DuplicateNameException(ExtractAttemptedName(ex));
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sqlEx &&
        (sqlEx.Number == UniqueConstraintViolation || sqlEx.Number == UniqueIndexViolation);

    private static string ExtractAttemptedName(DbUpdateException ex) =>
        (ex.Entries.Count > 0 && ex.Entries[0].Entity is Project p) ? p.Name : "(unknown)";
}
