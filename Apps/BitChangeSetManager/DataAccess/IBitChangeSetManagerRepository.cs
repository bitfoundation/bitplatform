using Foundation.DataAccess.Contracts;
using Foundation.Model.Contracts;

namespace BitChangeSetManager.DataAccess
{
    public interface IBitChangeSetManagerRepository<TEntity> : IEntityWithDefaultGuidKeyRepository<TEntity>
        where TEntity : class, IEntityWithDefaultGuidKey
    {
    }
}