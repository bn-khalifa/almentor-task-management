using Almentor.TaskApi.Domain.Enums;

namespace Almentor.TaskApi.Application.Features.Tasks.Dtos;

public class TaskResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }

    // every Task response carries it, not just the list-all endpoint
    public string ProjectName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; }
    public TaskItemPriority Priority { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
