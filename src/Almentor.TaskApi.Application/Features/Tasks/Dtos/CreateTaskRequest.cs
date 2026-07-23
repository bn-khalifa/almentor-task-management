using Almentor.TaskApi.Domain.Enums;

namespace Almentor.TaskApi.Application.Features.Tasks.Dtos;

// Status/Priority are nullable so an omitted field means "use the default" (Todo/Medium, applied in TaskService) 
public class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus? Status { get; set; }
    public TaskItemPriority? Priority { get; set; }
    public DateOnly? DueDate { get; set; }
}
