namespace Almentor.TaskApi.Application.Common.Models;

// A page of items plus the total count across all pages (required by the spec).
// Repositories build this from a single query pair (count + paged select), not
// by materializing the full set — see ProjectRepository.GetPagedAsync.
public class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Total { get; init; }
    public required int Offset { get; init; }
    public required int Limit { get; init; }
}
