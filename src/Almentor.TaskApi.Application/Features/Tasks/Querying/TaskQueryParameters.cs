namespace Almentor.TaskApi.Application.Features.Tasks.Querying;

/// <summary>
/// Raw task-list query, bound from the query string. The service parses this
/// into the typed <see cref="TaskListQuery"/> once validated.
/// </summary>
public class TaskQueryParameters
{
    // Filters
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public DateOnly? DueDateFrom { get; set; }
    public DateOnly? DueDateTo { get; set; }

    // Search (partial match on title/description (case-insensitive via DB collation))
    public string? Q { get; set; }

    // Sorting
    public string? Sort { get; set; }       // created_at | due_date | priority
    public string? Direction { get; set; }  // asc | desc

    // Pagination 
    public int Offset { get; set; }
    public int Limit { get; set; }
}
