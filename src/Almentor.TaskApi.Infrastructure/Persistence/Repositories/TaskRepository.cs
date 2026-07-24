using Almentor.TaskApi.Application.Common.Interfaces;
using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Application.Features.Tasks.Querying;
using Almentor.TaskApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Almentor.TaskApi.Infrastructure.Persistence.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _context.Tasks
            .Include(t => t.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<PagedResult<TaskItem>> GetPagedAsync(TaskListQuery query, CancellationToken ct)
    {
        // Include(Project): one JOIN so every row carries its project name 
        IQueryable<TaskItem> tasks = _context.Tasks
            .Include(t => t.Project)
            .AsNoTracking();

        tasks = ApplyFilters(tasks, query);
        tasks = ApplySort(tasks, query.Sort, query.Direction);

        // Count runs against the filtered-but-unpaged set;
        var total = await tasks.CountAsync(ct);

        var items = await tasks
            .Skip(query.Pagination.Offset)
            .Take(query.Pagination.Limit)
            .ToListAsync(ct);

        return new PagedResult<TaskItem>
        {
            Items = items,
            Total = total,
            Offset = query.Pagination.Offset,
            Limit = query.Pagination.Limit
        };
    }

    private static IQueryable<TaskItem> ApplyFilters(IQueryable<TaskItem> tasks, TaskListQuery query)
    {
        if (query.ProjectId is not null)
        {
            tasks = tasks.Where(t => t.ProjectId == query.ProjectId.Value);
        }

        if (query.Status is not null)
        {
            tasks = tasks.Where(t => t.Status == query.Status.Value);
        }

        if (query.Priority is not null)
        {
            tasks = tasks.Where(t => t.Priority == query.Priority.Value);
        }

        // Range filters skip null-dated tasks, which is the intended behavior
        if (query.DueDateFrom is not null)
        {
            tasks = tasks.Where(t => t.DueDate >= query.DueDateFrom.Value);
        }

        if (query.DueDateTo is not null)
        {
            tasks = tasks.Where(t => t.DueDate <= query.DueDateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // LIKE %term% — partial match, case-insensitive via SQL Server default collation
            var term = $"%{query.Search}%";
            tasks = tasks.Where(t =>
                EF.Functions.Like(t.Title, term) ||
                (t.Description != null && EF.Functions.Like(t.Description, term)));
        }

        return tasks;
    }

    private static IQueryable<TaskItem> ApplySort(
        IQueryable<TaskItem> tasks, TaskSortField sort, SortDirection direction)
    {
        var ascending = direction == SortDirection.Asc;

        IOrderedQueryable<TaskItem> ordered = sort switch
        {
            // NULLs last in both directions
            TaskSortField.DueDate => ascending
                ? tasks.OrderBy(t => t.DueDate == null).ThenBy(t => t.DueDate)
                : tasks.OrderBy(t => t.DueDate == null).ThenByDescending(t => t.DueDate),

            // Priority is tinyint, so this sorts semantically (Low<Medium<High)
            TaskSortField.Priority => ascending
                ? tasks.OrderBy(t => t.Priority)
                : tasks.OrderByDescending(t => t.Priority),

            _ => ascending
                ? tasks.OrderBy(t => t.CreatedAt)
                : tasks.OrderByDescending(t => t.CreatedAt)
        };

        // Stable tiebreaker: without a unique final key, rows equal on the sort
        // column could reorder between pages, causing skips/duplicates under offset paging.
        return ordered.ThenBy(t => t.Id);
    }

    public async Task AddAsync(TaskItem task, CancellationToken ct)
    {
        task.Id = Guid.NewGuid();
        await _context.Tasks.AddAsync(task, ct);
    }

    public void Update(TaskItem task) =>
        // Entry(task).State, not DbSet.Update(task): GetByIdAsync Include()s the
        // Project navigation, and Update() walks the whole reachable graph,
        // which would also mark the (untouched) Project as Modified
        _context.Entry(task).State = EntityState.Modified;

    public void Remove(TaskItem task) =>
        _context.Entry(task).State = EntityState.Deleted;

    public Task SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
}
