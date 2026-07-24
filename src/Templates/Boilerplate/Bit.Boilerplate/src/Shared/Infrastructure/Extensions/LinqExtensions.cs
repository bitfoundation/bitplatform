using System.Linq.Expressions;

namespace System.Collections.Generic;

public static partial class LinqExtensions
{
    extension<T>(IQueryable<T> query)
    {
        /// <summary>  
        /// </summary>  
        public IQueryable<T> WhereIf(bool predicate, Expression<Func<T, bool>> itemPredicate)
        {
            return predicate ? query.Where(itemPredicate) : query;
        }

        public IQueryable<T> OrderByIf<TKey>(bool predicate, Expression<Func<T, TKey>> keySelector)
        {
            return predicate ? query.OrderBy(keySelector) : query;
        }

        public IQueryable<T> OrderByDescendingIf<TKey>(bool predicate, Expression<Func<T, TKey>> keySelector)
        {
            return predicate ? query.OrderByDescending(keySelector) : query;
        }

        public IQueryable<T> SkipIf(bool predicate, int count)
        {
            return predicate ? query.Skip(count) : query;
        }

        public IQueryable<T> TakeIf(bool predicate, int count)
        {
            return predicate ? query.Take(count) : query;
        }

        public IQueryable<T> SkipIf(bool predicate, int? count)
        {
            return (predicate && count.HasValue) ? query.Skip(count.Value) : query;
        }

        public IQueryable<T> TakeIf(bool predicate, int? count)
        {
            return (predicate && count.HasValue) ? query.Take(count.Value) : query;
        }
    }

    extension<T>(IEnumerable<T> source)
    {
        public IEnumerable<T> WhereIf(bool predicate, Func<T, bool> itemPredicate)
        {
            return predicate ? source.Where(itemPredicate) : source;
        }

        public IEnumerable<T> OrderByIf<TKey>(bool predicate, Func<T, TKey> keySelector)
        {
            return predicate ? source.OrderBy(keySelector) : source;
        }

        public IEnumerable<T> OrderByDescendingIf<TKey>(bool predicate, Func<T, TKey> keySelector)
        {
            return predicate ? source.OrderByDescending(keySelector) : source;
        }

        public IEnumerable<T> SkipIf(bool predicate, int count)
        {
            return predicate ? source.Skip(count) : source;
        }

        public IEnumerable<T> TakeIf(bool predicate, int count)
        {
            return predicate ? source.Take(count) : source;
        }

        public IEnumerable<T> SkipIf(bool predicate, int? count)
        {
            return (predicate && count.HasValue) ? source.Skip(count.Value) : source;
        }

        public IEnumerable<T> TakeIf(bool predicate, int? count)
        {
            return (predicate && count.HasValue) ? source.Take(count.Value) : source;
        }
    }
}
