using Almentor.TaskApi.Domain.Common;
using Almentor.TaskApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Almentor.TaskApi.Infrastructure.Persistence;

// The Entity Framework Core unit of work for the API.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Pick up every IEntityTypeConfiguration in this assembly (ProjectConfiguration, TaskItemConfiguration, and so on)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        StampAuditFields();
        return base.SaveChanges();
    }

    /*
       Sets CreatedAt/UpdatedAt on any tracked auditable entity so callers never
       have to. CreatedAt is written once on insert and protected from later edits.
    */
    private void StampAuditFields()
    {
        var nowUtc = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = nowUtc;
                    entry.Entity.UpdatedAt = nowUtc;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = nowUtc;
                    // An update must never rewrite the original creation time.
                    entry.Property(nameof(AuditableEntity.CreatedAt)).IsModified = false;
                    break;
            }
        }
    }
}
