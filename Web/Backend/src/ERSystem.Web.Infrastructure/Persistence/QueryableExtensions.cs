using ERSystem.Web.Application.Common;

namespace ERSystem.Web.Infrastructure.Persistence;

public static class QueryableExtensions
{
    public static PagedResult<T> ToInMemoryPagedResult<T>(this IReadOnlyList<T> source, PagedRequest request)
    {
        var items = source.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToArray();
        return new PagedResult<T>(items, source.Count, request.Page, request.PageSize);
    }

    public static IQueryable<T> ApplyValidatedSort<T>(
        this IQueryable<T> source,
        string? requestedSort,
        SortDirection direction,
        IReadOnlyDictionary<string, Func<IQueryable<T>, SortDirection, IQueryable<T>>> allowedSorts,
        Func<IQueryable<T>, IQueryable<T>> defaultSort)
    {
        if (requestedSort is not null && allowedSorts.TryGetValue(requestedSort, out var apply))
            return apply(source, direction);
        return defaultSort(source);
    }

}
