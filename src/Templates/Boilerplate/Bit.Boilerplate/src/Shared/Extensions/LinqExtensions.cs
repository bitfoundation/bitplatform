using System.Linq.Expressions;

namespace System.Collections.Generic;

public static partial class LinqExtensions
{
    /// <summary>  
    /// </summary>  
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool predicate, Expression<Func<T, bool>> itemPredicate)
    {
        return predicate ? query.Where(itemPredicate) : query;
    }

    public static IQueryable<T> OrderByIf<T, TKey>(this IQueryable<T> query, bool predicate, Expression<Func<T, TKey>> keySelector)
    {
        return predicate ? query.OrderBy(keySelector) : query;
    }

    public static IQueryable<T> OrderByDescendingIf<T, TKey>(this IQueryable<T> query, bool predicate, Expression<Func<T, TKey>> keySelector)
    {
        return predicate ? query.OrderByDescending(keySelector) : query;
    }

    public static IQueryable<T> SkipIf<T>(this IQueryable<T> query, bool predicate, int count)
    {
        return predicate ? query.Skip(count) : query;
    }

    public static IQueryable<T> TakeIf<T>(this IQueryable<T> query, bool predicate, int count)
    {
        return predicate ? query.Take(count) : query;
    }

    public static IQueryable<T> SkipIf<T>(this IQueryable<T> query, bool predicate, int? count)
    {
        return (predicate && count.HasValue) ? query.Skip(count.Value) : query;
    }

    public static IQueryable<T> TakeIf<T>(this IQueryable<T> query, bool predicate, int? count)
    {
        return (predicate && count.HasValue) ? query.Take(count.Value) : query;
    }

    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool predicate, Func<T, bool> itemPredicate)
    {
        return predicate ? source.Where(itemPredicate) : source;
    }

    public static IEnumerable<T> OrderByIf<T, TKey>(this IEnumerable<T> source, bool predicate, Func<T, TKey> keySelector)
    {
        return predicate ? source.OrderBy(keySelector) : source;
    }

    public static IEnumerable<T> OrderByDescendingIf<T, TKey>(this IEnumerable<T> source, bool predicate, Func<T, TKey> keySelector)
    {
        return predicate ? source.OrderByDescending(keySelector) : source;
    }

    public static IEnumerable<T> SkipIf<T>(this IEnumerable<T> source, bool predicate, int count)
    {
        return predicate ? source.Skip(count) : source;
    }

    public static IEnumerable<T> TakeIf<T>(this IEnumerable<T> source, bool predicate, int count)
    {
        return predicate ? source.Take(count) : source;
    }

    public static IEnumerable<T> SkipIf<T>(this IEnumerable<T> source, bool predicate, int? count)
    {
        return (predicate && count.HasValue) ? source.Skip(count.Value) : source;
    }

    public static IEnumerable<T> TakeIf<T>(this IEnumerable<T> source, bool predicate, int? count)
    {
        return (predicate && count.HasValue) ? source.Take(count.Value) : source;
    }
}
