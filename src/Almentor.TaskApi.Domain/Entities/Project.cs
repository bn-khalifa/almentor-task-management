using Almentor.TaskApi.Domain.Common;

namespace Almentor.TaskApi.Domain.Entities;

public class Project : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    // Set when the project is soft-deleted; null while live (see ISoftDeletable).
    public DateTime? DeletedAt { get; set; }

    // Navigation property for the tasks associated with this project
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
