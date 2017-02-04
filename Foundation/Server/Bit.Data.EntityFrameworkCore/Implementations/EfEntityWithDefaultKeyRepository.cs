using Foundation.DataAccess.Contracts;
using Foundation.Model.Contracts;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq.Dynamic.Core;

namespace Foundation.DataAccess.Implementations.EntityFrameworkCore
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
            return GetAll().Where("Id = @0", key)
               .FirstOrDefault();
        }

        public virtual async Task<TEntity> GetByIdAsync(TKey key, CancellationToken cancellationToken)
        {
            return await GetAll().Where("Id = @0", key)
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
            throw new NotImplementedException();
        }
    }
}
