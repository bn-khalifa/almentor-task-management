using Almentor.TaskApi.Domain.Enums;

namespace Almentor.TaskApi.Application.Features.Tasks.Dtos;

public class UpdateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus? Status { get; set; }
    public TaskItemPriority? Priority { get; set; }
    public DateOnly? DueDate { get; set; }
}
