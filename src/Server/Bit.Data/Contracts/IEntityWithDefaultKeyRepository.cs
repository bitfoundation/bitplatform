using System.Threading;
using System.Threading.Tasks;
using Bit.Model.Contracts;

namespace Bit.Data.Contracts
{
    public interface IEntityWithDefaultKeyRepository<TEntity, TKey> : IRepository<TEntity>
        where TEntity : class, IEntityWithDefaultKey<TKey>
    {
        TKey GetNewKey();

        Task<TEntity> GetByIdAsync(TKey key, CancellationToken cancellationToken);

        TEntity GetById(TKey key);
    }
}
