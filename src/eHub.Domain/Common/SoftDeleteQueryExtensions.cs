namespace eHub.Domain.Common;

public static class SoftDeleteQueryExtensions
{
    public static IQueryable<T> WhereNotDeleted<T>(this IQueryable<T> query)
        where T : class, ISoftDeletable
        => query.Where(entity => !entity.IsDeleted);

    public static IEnumerable<T> WhereNotDeleted<T>(this IEnumerable<T> source)
        where T : class, ISoftDeletable
        => source.Where(entity => !entity.IsDeleted);
}
