using System.Collections.Generic;
using Bit.Data.Contracts;
using Bit.Model.Contracts;

namespace System.Linq
{
    public static class IWhereByKeyBuildersExtensions
    {
        public static IQueryable<TSource> WhereByKey<TSource, TKey>(this IEnumerable<IWhereByKeyBuilder<TSource, TKey>> whereByKeyBuilders, IQueryable<TSource> query, TKey key)
            where TSource : class, IWithDefaultKey<TKey>
        {
            if (whereByKeyBuilders == null)
                throw new ArgumentNullException(nameof(whereByKeyBuilders));

            if (query == null)
                throw new ArgumentNullException(nameof(query));

            IWhereByKeyBuilder<TSource, TKey> whereByKeyBuilder = whereByKeyBuilders
                .SingleOrDefault(w => w.CanBuildWhereByKeyQuery(query));

            if (whereByKeyBuilder != null)
                query = whereByKeyBuilder.WhereByKey(query, key);
            else
                query = CallDefaultWhereByKey(query, key);

            return query;
        }

        private static IQueryable<TSource> CallDefaultWhereByKey<TSource, TKey>(this IQueryable<TSource> query, TKey key)
            where TSource : class, IWithDefaultKey<TKey>
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            query = query.WhereByKey(key);

            return query;
        }
    }
}
