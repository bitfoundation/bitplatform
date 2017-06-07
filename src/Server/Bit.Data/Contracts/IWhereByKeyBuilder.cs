using System.Linq;
using Bit.Model.Contracts;

namespace Bit.Data.Contracts
{
    public interface IWhereByKeyBuilder<TSource, in TKey>
        where TSource : class, IWithDefaultKey<TKey>
    {
        bool CanBuildWhereByKeyQuery(IQueryable<TSource> query);

        IQueryable<TSource> WhereByKey(IQueryable<TSource> query, TKey key);
    }
}
