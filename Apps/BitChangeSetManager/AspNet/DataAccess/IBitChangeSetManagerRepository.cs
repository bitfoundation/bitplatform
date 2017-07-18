using Bit.Data.Contracts;
using Bit.Model.Contracts;

namespace BitChangeSetManager.DataAccess
{
    public interface IBitChangeSetManagerRepository<TEntity> : IEntityWithDefaultGuidKeyRepository<TEntity>
        where TEntity : class, IEntityWithDefaultGuidKey
    {
    }
}