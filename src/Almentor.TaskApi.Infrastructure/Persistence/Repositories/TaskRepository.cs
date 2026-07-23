using Almentor.TaskApi.Application.Common.Interfaces;
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
