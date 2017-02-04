using Foundation.Model.Contracts;
using System.Linq;

namespace Foundation.DataAccess.Contracts
{
    public interface IWhereByKeyBuilder<TSource, TKey>
        where TSource : class, IWithDefaultKey<TKey>
    {
        bool CanBuildWhereByKeyQuery(IQueryable<TSource> query);

        IQueryable<TSource> WhereByKey(IQueryable<TSource> query, TKey key);
    }
}
