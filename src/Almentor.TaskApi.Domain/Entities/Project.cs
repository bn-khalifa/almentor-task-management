using Almentor.TaskApi.Domain.Common;

namespace Almentor.TaskApi.Domain.Entities;

public class Project : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    // The user who owns this project; tasks inherit ownership through it.
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    // Set when the project is soft-deleted; null while live (see ISoftDeletable).
    public DateTime? DeletedAt { get; set; }

    // Navigation property for the tasks associated with this project
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
