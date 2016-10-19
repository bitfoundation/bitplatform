using Foundation.DataAccess.Contracts;
using Foundation.DataAccess.Implementations.EntityFramework;
using Foundation.Model.Contracts;
using System;

namespace Foundation.DataAccess.Implementations
{
    public class EfEntityWithDefaultGuidKeyRepository<TEntity> : EfEntityWithDefaultKeyRepository<TEntity, Guid>, IEntityWithDefaultGuidKeyRepository<TEntity>
        where TEntity : class, IEntityWithDefaultGuidKey
    {
        protected EfEntityWithDefaultGuidKeyRepository()
            : base()
        {
        }

        protected EfEntityWithDefaultGuidKeyRepository(DbContextBase dbContext)
            : base(dbContext)
        {
        }

        public override Guid GetNewKey()
        {
            return Guid.NewGuid();
        }
    }
}
