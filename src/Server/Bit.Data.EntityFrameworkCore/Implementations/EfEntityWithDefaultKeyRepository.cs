using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading;
using System.Threading.Tasks;
using Bit.Data.Contracts;
using Bit.Model.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Bit.Data.EntityFrameworkCore.Implementations
{
    public class EfEntityWithDefaultKeyRepository<TEntity, TKey> : EfRepository<TEntity>, IEntityWithDefaultKeyRepository<TEntity, TKey>
            where TEntity : class, IEntityWithDefaultKey<TKey>
    {
#if DEBUG
        protected EfEntityWithDefaultKeyRepository()
        {
        }
#endif

        protected EfEntityWithDefaultKeyRepository(DbContextBase dbContext)
            : base(dbContext)
        {
        }

        public virtual TEntity GetById(TKey key)
        {
            return GetAll().Where("Id = @0", key)
               .FirstOrDefault();
        }

        public virtual async Task<TEntity> GetByIdAsync(TKey key, CancellationToken cancellationToken)
        {
            return await (await GetAllAsync(cancellationToken)).Where("Id = @0", key)
               .FirstOrDefaultAsync(cancellationToken);
        }

        public override TEntity Add(TEntity entityToAdd)
        {
            if (EqualityComparer<TKey>.Default.Equals(entityToAdd.Id, default(TKey)))
                entityToAdd.Id = GetNewKey();

            return base.Add(entityToAdd);
        }

        public override Task<TEntity> AddAsync(TEntity entityToAdd, CancellationToken cancellationToken)
        {
            if (EqualityComparer<TKey>.Default.Equals(entityToAdd.Id, default(TKey)))
                entityToAdd.Id = GetNewKey();

            return base.AddAsync(entityToAdd, cancellationToken);
        }

        public virtual TKey GetNewKey()
        {
            return default(TKey);
        }
    }
}
