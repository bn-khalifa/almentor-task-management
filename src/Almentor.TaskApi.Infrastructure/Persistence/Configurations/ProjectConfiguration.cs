using Almentor.TaskApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Almentor.TaskApi.Infrastructure.Persistence.Configurations;

/*
   Maps to the Projects table: column rules, the unique
   name constraint, and the one-to-many relationship to tasks.
*/
public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        // DB-level enforcement of "duplicate project names should be rejected".
        // Filtered on live rows only, so a soft-deleted project's name is freed
        // up for reuse rather than being reserved forever.
        builder.HasIndex(p => p.Name)
            .IsUnique()
            .HasFilter("[DeletedAt] IS NULL")
            .HasDatabaseName("UX_Projects_Name");

        // One project owns many tasks; deleting the project cascade deletes them.
        builder.HasMany(p => p.Tasks)
            .WithOne(t => t.Project)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
