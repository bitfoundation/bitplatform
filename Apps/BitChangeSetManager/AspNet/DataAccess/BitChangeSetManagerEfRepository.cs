using Bit.Data.Contracts;
using Bit.Data.EntityFramework.Implementations;
using Bit.Model.Contracts;

namespace BitChangeSetManager.DataAccess
{
    public class BitChangeSetManagerEfRepository<TEntity> : EfRepository<TEntity>, IRepository<TEntity>
        where TEntity : class, IEntity
    {

    }
}