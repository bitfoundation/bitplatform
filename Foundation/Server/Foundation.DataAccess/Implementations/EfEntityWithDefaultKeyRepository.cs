using Foundation.DataAccess.Contracts;
using Foundation.Model.Contracts;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using System.Collections.Generic;
using Foundation.DataAccess.Implementations.EntityFramework;

namespace Foundation.DataAccess.Implementations
{
    public class EfEntityWithDefaultKeyRepository<TEntity, TKey> : EfRepository<TEntity>, IEntityWithDefaultKeyRepository<TEntity, TKey>
        where TEntity : class, IEntityWithDefaultKey<TKey>
        where TKey : struct
    {
        protected EfEntityWithDefaultKeyRepository()
            : base()
        {
        }

        protected EfEntityWithDefaultKeyRepository(DbContextBase dbContext)
            : base(dbContext)
        {
        }

        public virtual TEntity GetById(TKey key)
        {
            return GetAll()
               .Where($@"{nameof(IEntityWithDefaultGuidKey.Id)}.ToString()=""{key.ToString()}""")
               .SingleOrDefault();
        }

        public virtual async Task<TEntity> GetByIdAsync(TKey key, CancellationToken cancellationToken)
        {
            return await GetAll()
               .Where($@"{nameof(IEntityWithDefaultGuidKey.Id)}.ToString()=""{key.ToString()}""")
               .SingleOrDefaultAsync(cancellationToken);
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
