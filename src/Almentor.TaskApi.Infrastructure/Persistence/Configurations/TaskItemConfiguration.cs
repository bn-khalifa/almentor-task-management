using Almentor.TaskApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Almentor.TaskApi.Infrastructure.Persistence.Configurations;

/*
    Maps to the Tasks table: enum-as-string storage with defaults,
    indexes for filtering/sorting, and DB-level value guards.
*/
public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks", t =>
        {
            // The DB itself rejects any value outside the enum, even if a bad write bypasses the application layer.
            t.HasCheckConstraint("CK_Tasks_Status", "[Status] IN ('Todo', 'InProgress', 'Done')");
            t.HasCheckConstraint("CK_Tasks_Priority", "[Priority] IN (0, 1, 2)");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        // Status: readable string name. Only ever filtered by equality, never sorted,
        // so alphabetical storage is fine. Default comes from the entity initializer.
        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Priority: stored as tinyint (0=Low, 1=Medium, 2=High) so ORDER BY sorts
        // semantically (Low < Medium < High) straight off the index. Default comes
        // from the entity initializer. API still exposes low/medium/high via the DTO.
        builder.Property(t => t.Priority)
            .HasConversion<byte>()
            .IsRequired();

        // DateOnly maps to SQL Server 'date'
        builder.Property(t => t.DueDate)
            .HasColumnType("date");

        // Indexes backing the required filters and sorts on the task lists.
        builder.HasIndex(t => t.ProjectId).HasDatabaseName("IX_Tasks_ProjectId");
        builder.HasIndex(t => t.Status).HasDatabaseName("IX_Tasks_Status");
        builder.HasIndex(t => t.Priority).HasDatabaseName("IX_Tasks_Priority");
        builder.HasIndex(t => t.DueDate).HasDatabaseName("IX_Tasks_DueDate");
    }
}
