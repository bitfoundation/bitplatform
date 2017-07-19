using Bit.Data.EntityFramework.Implementations;
using Bit.Model.Contracts;

namespace BitChangeSetManager.DataAccess
{
    public class BitChangeSetManagerEfRepository<TEntity> : EfRepository<TEntity>, IBitChangeSetManagerRepository<TEntity>
        where TEntity : class, IEntity
    {
        public BitChangeSetManagerEfRepository(BitChangeSetManagerDbContext dbContext)
            : base(dbContext)
        {
        }
    }
}