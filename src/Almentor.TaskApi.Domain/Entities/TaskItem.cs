using Almentor.TaskApi.Domain.Common;
using Almentor.TaskApi.Domain.Enums;

namespace Almentor.TaskApi.Domain.Entities;

public class TaskItem : AuditableEntity, ISoftDeletable
{
    /// Foreign key to the owning project (required)
    public Guid ProjectId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;

    public TaskItemPriority Priority { get; set; } = TaskItemPriority.Medium;

    // Optional deadline
    public DateOnly? DueDate { get; set; }

    // Set when the task is soft-deleted; null while live (see ISoftDeletable).
    public DateTime? DeletedAt { get; set; }

    // Navigation back to the owning project
    public Project Project { get; set; } = null!;
}
