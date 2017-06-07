using System;
using Bit.Data.Contracts;
using Bit.Model.Contracts;

namespace Bit.Data.EntityFramework.Implementations
{
    public class EfEntityWithDefaultGuidKeyRepository<TEntity> : EfEntityWithDefaultKeyRepository<TEntity, Guid>, IEntityWithDefaultGuidKeyRepository<TEntity>
        where TEntity : class, IEntityWithDefaultGuidKey
    {
        protected EfEntityWithDefaultGuidKeyRepository()
            : base()
        {
        }

        protected EfEntityWithDefaultGuidKeyRepository(DefaultDbContext dbContext)
            : base(dbContext)
        {
        }

        public override Guid GetNewKey()
        {
            return Guid.NewGuid();
        }
    }
}
