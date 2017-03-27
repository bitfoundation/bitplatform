using Foundation.Model.Contracts;
using System.Linq.Dynamic;

namespace System.Linq
{
    public static class IQueryableExtensions
    {
        /// <summary>
        /// Either use this method (and System.Linq.Dynamic) or register your own IWhereByKeyBuilder's implementation
        /// </summary>
        public static IQueryable<TSource> WhereByKey<TSource, TKey>(this IQueryable<TSource> query, TKey key)
            where TSource : class, IWithDefaultKey<TKey>
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            query = query.Where("Id = @0", key);

            return query;
        }
    }
}
