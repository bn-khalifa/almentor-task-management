namespace Almentor.TaskApi.Application.Features.Tasks.Querying;

// Fields a task list may be sorted by
public enum TaskSortField
{
    CreatedAt,
    DueDate,
    Priority
}

// Sort direction
public enum SortDirection
{
    Asc,
    Desc
}
