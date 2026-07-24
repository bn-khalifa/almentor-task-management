namespace Almentor.TaskApi.Application.Common.Models;

// Shapes a PagedResult<T> into the standard ApiResponse<T>
public static class PagedResultExtensions
{
    public static ApiResponse<IReadOnlyList<T>> ToApiResponse<T>(this PagedResult<T> page) =>
        ApiResponse<IReadOnlyList<T>>.Ok(
            page.Items,
            new ResponseMeta
            {
                Pagination = new PaginationMeta
                {
                    Total = page.Total,
                    Offset = page.Offset,
                    Limit = page.Limit
                }
            });
}
