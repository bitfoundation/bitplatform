using Foundation.DataAccess.Contracts;
using Foundation.Model.Contracts;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace Foundation.DataAccess.Implementations.EntityFrameworkCore
{
    public class EfCoreWhereByKeyBuilder<TSource, TKey> : IWhereByKeyBuilder<TSource, TKey>
        where TSource : class, IWithDefaultKey<TKey>
    {
        public virtual IQueryable<TSource> WhereByKey(IQueryable<TSource> query, TKey key)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            query = query.Where("Id = @0", key);

            return query;
        }

        public virtual bool CanBuildWhereByKeyQuery(IQueryable<TSource> query)
        {
            return query is EntityQueryable<TSource>;
        }
    }
}
