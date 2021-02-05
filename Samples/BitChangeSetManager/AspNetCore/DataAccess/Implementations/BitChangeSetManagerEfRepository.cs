using Bit.Data.Contracts;
using Bit.Data.EntityFramework.Implementations;
using Bit.Model.Contracts;
using BitChangeSetManager.DataAccess.Contracts;

namespace BitChangeSetManager.DataAccess.Implementations
{
    public class BitChangeSetManagerEfRepository<TEntity> : EfRepository<TEntity>, IBitChangeSetManagerRepository<TEntity>
        where TEntity : class, IEntity
    {

    }
}