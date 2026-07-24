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

        // "Duplicate project names should be rejected" — but per owner, not
        // globally: two different users may each have a "Website" project.
        // Composite (OwnerId, Name), filtered to live rows so a soft-deleted
        // name is freed for reuse.
        builder.HasIndex(p => new { p.OwnerId, p.Name })
            .IsUnique()
            .HasFilter("[DeletedAt] IS NULL")
            .HasDatabaseName("UX_Projects_Owner_Name");

        // Each project belongs to one owner. Restrict (not cascade): there's no
        // user-deletion path, and we never want deleting a user to silently
        // wipe their projects.
        builder.HasOne(p => p.Owner)
            .WithMany(u => u.Projects)
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Backs the owner-scoped list/name queries.
        builder.HasIndex(p => p.OwnerId).HasDatabaseName("IX_Projects_OwnerId");

        // One project owns many tasks; deleting the project cascade deletes them.
        builder.HasMany(p => p.Tasks)
            .WithOne(t => t.Project)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
