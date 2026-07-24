using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Domain.Enums;

namespace Almentor.TaskApi.Application.Features.Tasks.Querying;

// Fully-typed, validated task-list query handed to the repository. Distinct from
// TaskQueryParameters (the raw HTTP shape) so the data layer never
// deals with parsing or invalid input — only real enum/date values.
public class TaskListQuery
{
    // The owner whose tasks to return — always set; a user only sees their own.
    public Guid OwnerId { get; init; }
    public Guid? ProjectId { get; init; }
    public TaskItemStatus? Status { get; init; }
    public TaskItemPriority? Priority { get; init; }
    public DateOnly? DueDateFrom { get; init; }
    public DateOnly? DueDateTo { get; init; }
    public string? Search { get; init; }
    public TaskSortField Sort { get; init; }
    public SortDirection Direction { get; init; }
    public PaginationParams Pagination { get; init; } = new();
}
