namespace ERFWeb.Application.Common;

public enum SortDirection
{
    Asc,
    Desc,
}

/// <summary>Standard paged request. Page is 1-based; size is clamped to [1, 100].</summary>
public record PagedRequest
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = DefaultPageSize;
    public string? Sort { get; init; }
    public SortDirection Direction { get; init; } = SortDirection.Asc;

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize => PageSize switch
    {
        < 1 => DefaultPageSize,
        > MaxPageSize => MaxPageSize,
        _ => PageSize,
    };

    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}

/// <summary>A single page of results plus the total count for the query.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);

    public static PagedResult<T> Empty(PagedRequest request) =>
        new([], 0, request.NormalizedPage, request.NormalizedPageSize);
}
