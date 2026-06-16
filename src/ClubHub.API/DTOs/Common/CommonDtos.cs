namespace ClubHub.API.DTOs.Common;

public record PagedResult<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalCount
)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrev => Page > 1;
}

public record ApiResponse<T>(bool Success, string? Message, T? Data);

public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T data, string? message = null)
        => new(true, message, data);

    public static ApiResponse<object> Fail(string message)
        => new(false, message, null);
}
