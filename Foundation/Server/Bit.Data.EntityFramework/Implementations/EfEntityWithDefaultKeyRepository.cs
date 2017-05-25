using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Data.Entity;
using Bit.Data.Contracts;
using Bit.Model.Contracts;

namespace Bit.Data.EntityFramework.Implementations
{
    public class EfEntityWithDefaultKeyRepository<TEntity, TKey> : EfRepository<TEntity>, IEntityWithDefaultKeyRepository<TEntity, TKey>
        where TEntity : class, IEntityWithDefaultKey<TKey>
    {
        protected EfEntityWithDefaultKeyRepository()
            : base()
        {
        }

        protected EfEntityWithDefaultKeyRepository(DefaultDbContext dbContext)
            : base(dbContext)
        {
        }

        public virtual TEntity GetById(TKey key)
        {
            return GetAll().WhereByKey(key)
                .FirstOrDefault();
        }

        public virtual async Task<TEntity> GetByIdAsync(TKey key, CancellationToken cancellationToken)
        {
            return await (await GetAllAsync(cancellationToken)).WhereByKey(key)
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
