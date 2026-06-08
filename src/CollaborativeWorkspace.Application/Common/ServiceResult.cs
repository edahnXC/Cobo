namespace CollaborativeWorkspace.Application.Common;

/// <summary>
/// Unified result wrapper to propagate business-layer outcomes without exceptions for expected failures.
/// </summary>
public class ServiceResult<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ErrorCode { get; private set; }
    public int StatusCode { get; private set; }

    private ServiceResult() { }

    public static ServiceResult<T> Success(T data, int statusCode = 200)
        => new() { IsSuccess = true, Data = data, StatusCode = statusCode };

    public static ServiceResult<T> Failure(string error, string errorCode = "ERROR", int statusCode = 400)
        => new() { IsSuccess = false, ErrorMessage = error, ErrorCode = errorCode, StatusCode = statusCode };

    public static ServiceResult<T> NotFound(string message = "Resource not found")
        => Failure(message, "NOT_FOUND", 404);

    public static ServiceResult<T> Unauthorized(string message = "Unauthorized")
        => Failure(message, "UNAUTHORIZED", 401);

    public static ServiceResult<T> Forbidden(string message = "Access denied")
        => Failure(message, "FORBIDDEN", 403);
}

public static class ServiceResult
{
    public static ServiceResult<object> Ok()
        => ServiceResult<object>.Success(new object(), 200);
}

/// <summary>
/// Standard paginated response wrapper.
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage => (Page * PageSize) < TotalCount;
    public bool HasPreviousPage => Page > 1;
}
