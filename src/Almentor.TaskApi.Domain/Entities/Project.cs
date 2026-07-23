using Almentor.TaskApi.Domain.Common;

namespace Almentor.TaskApi.Domain.Entities;

public class Project : AuditableEntity
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    // Navigation property for the tasks associated with this project
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
