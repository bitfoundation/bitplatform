using Bit.Data.Contracts;
using Bit.Model.Contracts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Data.EntityFramework.Implementations
{
    public class EfRepository<TEntity> : IRepository<TEntity>
        where TEntity : class, IEntity
    {
        private readonly EfDbContextBase _dbContext;
        private readonly DbSet<TEntity> _set;

#if DEBUG
        protected EfRepository()
        {
        }
#endif

        protected EfRepository(EfDbContextBase dbContext)
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));
            _dbContext = dbContext;
            _set = dbContext.Set<TEntity>();
        }

        public virtual async Task<TEntity> AddAsync(TEntity entityToAdd, CancellationToken cancellationToken)
        {
            if (entityToAdd == null)
                throw new ArgumentNullException(nameof(entityToAdd));

            IEntityWithDefaultGuidKey entityToAddAsEntityWithDefaultGuidKey = entityToAdd as IEntityWithDefaultGuidKey;
            if (entityToAddAsEntityWithDefaultGuidKey != null && entityToAddAsEntityWithDefaultGuidKey.Id == Guid.Empty)
                entityToAddAsEntityWithDefaultGuidKey.Id = Guid.NewGuid();

            _set.Add(entityToAdd);

            await SaveChangesAsync(cancellationToken);

            return entityToAdd;
        }

        public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entitiesToAdd, CancellationToken cancellationToken)
        {
            if (entitiesToAdd == null)
                throw new ArgumentNullException(nameof(entitiesToAdd));

            foreach (IEntity entityToAdd in entitiesToAdd)
            {
                IEntityWithDefaultGuidKey entityToAddAsEntityWithDefaultGuidKey = entityToAdd as IEntityWithDefaultGuidKey;
                if (entityToAddAsEntityWithDefaultGuidKey != null && entityToAddAsEntityWithDefaultGuidKey.Id == Guid.Empty)
                    entityToAddAsEntityWithDefaultGuidKey.Id = Guid.NewGuid();
            }

            _set.AddRange(entitiesToAdd);

            await SaveChangesAsync(cancellationToken);

            return entitiesToAdd;
        }

        public virtual async Task<TEntity> UpdateAsync(TEntity entityToUpdate, CancellationToken cancellationToken)
        {
            if (entityToUpdate == null)
                throw new ArgumentNullException(nameof(entityToUpdate));

            _set.Attach(entityToUpdate);
            _dbContext.Entry(entityToUpdate).State = EntityState.Modified;

            await SaveChangesAsync(cancellationToken);

            return entityToUpdate;
        }

        public virtual async Task<TEntity> DeleteAsync(TEntity entityToDelete, CancellationToken cancellationToken)
        {
            if (entityToDelete == null)
                throw new ArgumentNullException(nameof(entityToDelete));

            if (entityToDelete is IArchivableEntity)
            {
                ((IArchivableEntity)entityToDelete).IsArchived = true;
                return await UpdateAsync(entityToDelete, cancellationToken);
            }
            else
            {
                _set.Attach(entityToDelete);
                _dbContext.Entry(entityToDelete).State = EntityState.Deleted;
                await SaveChangesAsync(cancellationToken);
                return entityToDelete;
            }
        }

        public virtual bool IsChangedProperty<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> prop)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return _dbContext.Entry(entity).Property(prop).IsModified;
        }

        public virtual TProperty GetOriginalValue<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> prop)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return _dbContext.Entry(entity).Property(prop).OriginalValue;
        }

        public virtual bool IsDeleted(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return _dbContext.Entry(entity).State == EntityState.Deleted;
        }

        public virtual bool IsAdded(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return _dbContext.Entry(entity).State == EntityState.Added;
        }

        public virtual bool IsModified(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return _dbContext.Entry(entity).State != EntityState.Unchanged;
        }

        public virtual void Detach(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbContext.Entry(entity).State = EntityState.Detached;
        }

        public virtual void Attach(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _set.Attach(entity);
        }

        public virtual TEntity Add(TEntity entityToAdd)
        {
            if (entityToAdd == null)
                throw new ArgumentNullException(nameof(entityToAdd));

            IEntityWithDefaultGuidKey entityToAddAsEntityWithDefaultGuidKey = entityToAdd as IEntityWithDefaultGuidKey;
            if (entityToAddAsEntityWithDefaultGuidKey != null && entityToAddAsEntityWithDefaultGuidKey.Id == Guid.Empty)
                entityToAddAsEntityWithDefaultGuidKey.Id = Guid.NewGuid();

            _set.Add(entityToAdd);

            SaveChanges();

            return entityToAdd;
        }

        public virtual IEnumerable<TEntity> AddRange(IEnumerable<TEntity> entitiesToAdd)
        {
            if (entitiesToAdd == null)
                throw new ArgumentNullException(nameof(entitiesToAdd));

            foreach (IEntity entityToAdd in entitiesToAdd)
            {
                IEntityWithDefaultGuidKey entityToAddAsEntityWithDefaultGuidKey = entityToAdd as IEntityWithDefaultGuidKey;
                if (entityToAddAsEntityWithDefaultGuidKey != null && entityToAddAsEntityWithDefaultGuidKey.Id == Guid.Empty)
                    entityToAddAsEntityWithDefaultGuidKey.Id = Guid.NewGuid();
            }

            _set.AddRange(entitiesToAdd);

            SaveChanges();

            return entitiesToAdd;
        }

        public virtual TEntity Update(TEntity entityToUpdate)
        {
            if (entityToUpdate == null)
                throw new ArgumentNullException(nameof(entityToUpdate));

            _set.Attach(entityToUpdate);
            _dbContext.Entry(entityToUpdate).State = EntityState.Modified;

            SaveChanges();

            return entityToUpdate;
        }

        public virtual TEntity Delete(TEntity entityToDelete)
        {
            if (entityToDelete == null)
                throw new ArgumentNullException(nameof(entityToDelete));

            if (entityToDelete is IArchivableEntity)
            {
                ((IArchivableEntity)entityToDelete).IsArchived = true;
                return Update(entityToDelete);
            }
            else
            {
                _set.Attach(entityToDelete);
                _dbContext.Entry(entityToDelete).State = EntityState.Deleted;
                SaveChanges();
                return entityToDelete;
            }
        }

        public virtual IQueryable<TEntity> GetAll()
        {
            return _set.AsNoTracking();
        }

        public virtual async Task<IQueryable<TEntity>> GetAllAsync(CancellationToken cancellationToken)
        {
            return _set.AsNoTracking();
        }

        public virtual IQueryable<TChild> GetCollectionQuery<TChild>(TEntity entity, Expression<Func<TEntity, ICollection<TChild>>> childs) where TChild : class
        {
            return _dbContext.Entry(entity).Collection(childs).Query();
        }

        public virtual async Task LoadCollectionAsync<TProperty>(TEntity entity, Expression<Func<TEntity, ICollection<TProperty>>> childs, CancellationToken cancellationToken, bool forceReload = false) where TProperty : class
        {
            DbCollectionEntry<TEntity, TProperty> collection = _dbContext.Entry(entity).Collection(childs);

            if (forceReload == true || collection.IsLoaded == false)
                await collection.LoadAsync(cancellationToken);
        }

        public virtual void LoadCollection<TProperty>(TEntity entity, Expression<Func<TEntity, ICollection<TProperty>>> childs, bool forceReload = false) where TProperty : class
        {
            DbCollectionEntry<TEntity, TProperty> collection = _dbContext.Entry(entity).Collection(childs);

            if (forceReload == true || collection.IsLoaded == false)
                collection.Load();
        }

        public virtual async Task LoadReferenceAsync<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> member, CancellationToken cancellationToken, bool forceReload = false) where TProperty : class
        {
            DbReferenceEntry<TEntity, TProperty> reference = _dbContext.Entry(entity).Reference(member);
            if (forceReload == true || reference.IsLoaded == false)
                await reference.LoadAsync(cancellationToken);
        }

        public virtual void LoadReference<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> member, bool forceReload = false) where TProperty : class
        {
            DbReferenceEntry<TEntity, TProperty> reference = _dbContext.Entry(entity).Reference(member);
            if (forceReload == true || reference.IsLoaded == false)
                reference.Load();
        }

        public virtual async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            _dbContext.ChangeTracker.DetectChanges();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public virtual void SaveChanges()
        {
            _dbContext.ChangeTracker.DetectChanges();
            _dbContext.SaveChanges();
        }

        public virtual async Task<TEntity> GetByIdAsync(params object[] keys)
        {
            return await EfDataProviderSpecificMethodsProvider.ApplyWhereByKeys((await GetAllAsync(CancellationToken.None)), keys)
                .SingleAsync(CancellationToken.None);
        }

        public virtual TEntity GetById(params object[] keys)
        {
            return EfDataProviderSpecificMethodsProvider.ApplyWhereByKeys(GetAll(), keys)
                .Single();
        }

        public EfDataProviderSpecificMethodsProvider EfDataProviderSpecificMethodsProvider { get; set; }
    }
}