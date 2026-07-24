using System.Linq.Expressions;
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
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Pick up every IEntityTypeConfiguration in this assembly (ProjectConfiguration, TaskItemConfiguration, and so on)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ApplySoftDeleteQueryFilters(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    // Adds "WHERE DeletedAt IS NULL" to every ISoftDeletable entity's queries,
    // so soft-deleted rows disappear from all reads automatically — no repository
    // has to remember to filter. Built generically so any future soft-deletable
    // entity is covered without touching this method.
    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            // Build: (TEntity e) => e.DeletedAt == null
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var deletedAt = Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
            var isNull = Expression.Equal(deletedAt, Expression.Constant(null, typeof(DateTime?)));

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(Expression.Lambda(isNull, parameter));
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ConvertSoftDeletes();
        StampAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ConvertSoftDeletes();
        StampAuditFields();
        return base.SaveChanges();
    }

    // Turns hard deletes into soft deletes: any ISoftDeletable being removed is
    // flipped back to Modified with DeletedAt stamped instead. Runs before
    // StampAuditFields so the same rows also get their UpdatedAt bumped.
    private void ConvertSoftDeletes()
    {
        // Force EF's immediate cascade first, so when a project is removed its
        // LOADED tasks are already marked Deleted here and get soft-deleted too —
        // preserving the spec's cascade semantics without a hard delete.
        ChangeTracker.DetectChanges();

        var nowUtc = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.DeletedAt = nowUtc;
            }
        }
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
