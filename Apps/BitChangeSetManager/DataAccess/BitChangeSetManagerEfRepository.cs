using Bit.Data.EntityFramework.Implementations;
using Foundation.Model.Contracts;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace BitChangeSetManager.DataAccess
{
    public class BitChangeSetManagerEfRepository<TEntity> : EfEntityWithDefaultGuidKeyRepository<TEntity>, IBitChangeSetManagerRepository<TEntity>
        where TEntity : class, IEntityWithDefaultGuidKey
    {
        public BitChangeSetManagerEfRepository(BitChangeSetManagerDbContext dbContext)
            : base(dbContext)
        {
        }

        public override TEntity Add(TEntity entityToAdd)
        {
            OnAddingAsync(new[] { entityToAdd }).Wait();

            return base.Add(entityToAdd);
        }

        public override async Task<TEntity> AddAsync(TEntity entityToAdd, CancellationToken cancellationToken)
        {
            await OnAddingAsync(new[] { entityToAdd });

            return await base.AddAsync(entityToAdd, cancellationToken);
        }

        public override IEnumerable<TEntity> AddRange(IEnumerable<TEntity> entitiesToAdd)
        {
            OnAddingAsync(entitiesToAdd).Wait();

            return base.AddRange(entitiesToAdd);
        }

        public override async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entitiesToAdd, CancellationToken cancellationToken)
        {
            await OnAddingAsync(entitiesToAdd);

            return await base.AddRangeAsync(entitiesToAdd, cancellationToken);
        }

        protected virtual async Task OnAddingAsync(IEnumerable<TEntity> entities)
        {

        }

        public override TEntity Update(TEntity entityToUpdate)
        {
            OnUpdatingAsync(new[] { entityToUpdate }).Wait();

            return base.Update(entityToUpdate);
        }

        public override async Task<TEntity> UpdateAsync(TEntity entityToUpdate, CancellationToken cancellationToken)
        {
            await OnUpdatingAsync(new[] { entityToUpdate });

            return await base.UpdateAsync(entityToUpdate, cancellationToken);
        }

        protected virtual async Task OnUpdatingAsync(IEnumerable<TEntity> entities)
        {

        }

        public override TEntity Delete(TEntity entityToDelete)
        {
            OnDeletingAsync(new[] { entityToDelete }).Wait();

            return base.Delete(entityToDelete);
        }

        public override async Task<TEntity> DeleteAsync(TEntity entityToDelete, CancellationToken cancellationToken)
        {
            await OnDeletingAsync(new[] { entityToDelete });

            return await base.DeleteAsync(entityToDelete, cancellationToken);
        }

        protected virtual async Task OnDeletingAsync(IEnumerable<TEntity> entities)
        {

        }
    }
}