namespace Almentor.TaskApi.Application.Common.Models;

// The single response shape every endpoint returns, success or failure, so
// clients never have to guess the JSON structure from the status code alone.
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public ErrorDetail? Error { get; init; }
    public ResponseMeta? Meta { get; init; }

    public static ApiResponse<T> Ok(T data, ResponseMeta? meta = null) =>
        new() { Success = true, Data = data, Meta = meta };

    public static ApiResponse<T> Fail(ErrorDetail error, ResponseMeta? meta = null) =>
        new() { Success = false, Error = error, Meta = meta };
}

// Non-generic helper so callers writing error responses (e.g. the exception
// middleware, which doesn't know T) don't have to specify a type argument.
public static class ApiResponse
{
    public static ApiResponse<object> Fail(ErrorDetail error, ResponseMeta? meta = null) =>
        ApiResponse<object>.Fail(error, meta);
}

// Machine-readable error code, human message, and optional per-field details
public class ErrorDetail
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public IReadOnlyList<FieldError>? Details { get; init; }
}

public class FieldError
{
    public required string Field { get; init; }
    public required string Message { get; init; }
}

// Envelope metadata: pagination on list responses, traceId on errors
public class ResponseMeta
{
    public PaginationMeta? Pagination { get; init; }
    public string? TraceId { get; init; }
}

public class PaginationMeta
{
    public required int Total { get; init; }
    public required int Offset { get; init; }
    public required int Limit { get; init; }
}
