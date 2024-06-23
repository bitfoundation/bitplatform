using System.Linq.Expressions;

namespace System.Collections.Generic;

public static class LinqExtensions
{
    /// <summary>
    /// https://extensionmethod.net/csharp/ienumerable-t/whereif
    /// </summary>
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool predicate, Expression<Func<T, bool>> itemPredicate)
    {
        return predicate ? query.Where(itemPredicate) : query;
    }

    public static IQueryable<T> OrderByIf<T>(this IQueryable<T> query, bool predicate, Expression<Func<T, object>> keySelector)
    {
        return predicate ? query.OrderBy(keySelector) : query;
    }

    public static IQueryable<T> OrderByDescendingIf<T>(this IQueryable<T> query, bool predicate, Expression<Func<T, object>> keySelector)
    {
        return predicate ? query.OrderByDescending(keySelector) : query;
    }

    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool predicate, Func<T, bool> itemPredicate)
    {
        return predicate ? source.Where(itemPredicate) : source;
    }

    public static IEnumerable<T> OrderByIf<T>(this IEnumerable<T> source, bool predicate, Func<T, object> keySelector)
    {
        return predicate ? source.OrderBy(keySelector) : source;
    }

    public static IEnumerable<T> OrderByDescendingIf<T>(this IEnumerable<T> source, bool predicate, Func<T, object> keySelector)
    {
        return predicate ? source.OrderByDescending(keySelector) : source;
    }
}
