using Bit.Data.Contracts;
using Bit.Model.Contracts;

namespace BitChangeSetManager.DataAccess
{
    public interface IBitChangeSetManagerRepository<TEntity> : IRepository<TEntity>
        where TEntity : class, IEntity
    {
    }
}