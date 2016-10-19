using Foundation.Model.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Foundation.DataAccess.Contracts
{
    public interface IEntityWithDefaultKeyRepository<TEntity, TKey> : IRepository<TEntity>
        where TEntity : class, IEntityWithDefaultKey<TKey>
        where TKey : struct
    {
        TKey GetNewKey();

        Task<TEntity> GetByIdAsync(TKey key, CancellationToken cancellationToken);

        TEntity GetById(TKey key);
    }
}
