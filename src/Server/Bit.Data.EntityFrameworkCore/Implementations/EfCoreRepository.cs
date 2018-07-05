using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Data.Contracts;
using Bit.Model.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Bit.Data.EntityFrameworkCore.Implementations
{
    public class EfCoreRepository<TEntity> : IRepository<TEntity>
        where TEntity : class, IEntity
    {
        private EfCoreDbContextBase _dbContext;
        public virtual EfCoreDbContextBase DbContext
        {
            get => _dbContext;
            set
            {
                _dbContext = value;
                _set = _dbContext.Set<TEntity>();
            }
        }

        private DbSet<TEntity> _set;

        protected virtual DbSet<TEntity> Set => _set;

        public virtual async Task<TEntity> AddAsync(TEntity entityToAdd, CancellationToken cancellationToken)
        {
            if (entityToAdd == null)
                throw new ArgumentNullException(nameof(entityToAdd));

            if (entityToAdd is IEntityWithDefaultGuidKey entityToAddAsEntityWithDefaultGuidKey && entityToAddAsEntityWithDefaultGuidKey.Id == Guid.Empty)
                entityToAddAsEntityWithDefaultGuidKey.Id = Guid.NewGuid();
            if (entityToAdd is IVersionableEntity versionableEntity)
                versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

            if (entityToAdd is ISyncableEntity syncableEntity)
            {
                object[] keys = DbContext.Model.FindEntityType(typeof(TEntity).GetTypeInfo())
                    .FindPrimaryKey()
                    .Properties
                    .Select(p => p.PropertyInfo.GetValue(syncableEntity))
                    .ToArray();

                TEntity entityIfExists = await GetByIdAsync(cancellationToken, keys).ConfigureAwait(false);

                if (entityIfExists != null)
                    return entityIfExists;
            }

            await DbContext.AddAsync(entityToAdd, cancellationToken).ConfigureAwait(false);

            await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return entityToAdd;
        }

        public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entitiesToAdd, CancellationToken cancellationToken)
        {
            if (entitiesToAdd == null)
                throw new ArgumentNullException(nameof(entitiesToAdd));

            List<TEntity> entitiesToAddList = entitiesToAdd as List<TEntity> ?? entitiesToAdd.ToList();

            foreach (TEntity entityToAdd in entitiesToAddList)
            {
                if (entityToAdd is IEntityWithDefaultGuidKey entityToAddAsEntityWithDefaultGuidKey && entityToAddAsEntityWithDefaultGuidKey.Id == Guid.Empty)
                    entityToAddAsEntityWithDefaultGuidKey.Id = Guid.NewGuid();
                if (entityToAdd is IVersionableEntity versionableEntity)
                    versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;
            }

            await DbContext.AddRangeAsync(entitiesToAddList, cancellationToken).ConfigureAwait(false);

            await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return entitiesToAddList;
        }

        public virtual async Task<TEntity> UpdateAsync(TEntity entityToUpdate, CancellationToken cancellationToken)
        {
            if (entityToUpdate == null)
                throw new ArgumentNullException(nameof(entityToUpdate));

            if (entityToUpdate is IVersionableEntity versionableEntity)
                versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

            Attach(entityToUpdate);
            DbContext.Entry(entityToUpdate).State = EntityState.Modified;

            await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            Detach(entityToUpdate);

            return entityToUpdate;
        }

        public virtual async Task<TEntity> DeleteAsync(TEntity entityToDelete, CancellationToken cancellationToken)
        {
            if (entityToDelete == null)
                throw new ArgumentNullException(nameof(entityToDelete));

            if (entityToDelete is IVersionableEntity versionableEntity)
                versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

            if (entityToDelete is IArchivableEntity archivableEntity)
            {
                archivableEntity.IsArchived = true;
                return await UpdateAsync(entityToDelete, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                DbContext.Remove(entityToDelete);
                await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return entityToDelete;
            }
        }

        public virtual bool IsChangedProperty<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> prop)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            DbContext.ChangeTracker.DetectChanges();

            Attach(entity);

            return DbContext.Entry(entity).Property(prop).IsModified;
        }

        public virtual TProperty GetOriginalValue<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> prop)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            DbContext.ChangeTracker.DetectChanges();

            Attach(entity);

            return DbContext.Entry(entity).Property(prop).OriginalValue;
        }

        public virtual bool IsDeleted(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            DbContext.ChangeTracker.DetectChanges();

            Attach(entity);

            return DbContext.Entry(entity).State == EntityState.Deleted;
        }

        public virtual bool IsAdded(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            DbContext.ChangeTracker.DetectChanges();

            Attach(entity);

            return DbContext.Entry(entity).State == EntityState.Added;
        }

        public virtual bool IsModified(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            DbContext.ChangeTracker.DetectChanges();

            Attach(entity);

            return DbContext.Entry(entity).State != EntityState.Unchanged;
        }

        public virtual void Detach(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            Attach(entity);

            DbContext.Entry(entity).State = EntityState.Detached;
        }

        public virtual void Attach(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (DbContext.Entry(entity).State == EntityState.Detached)
                Set.Attach(entity);
        }

        public virtual TEntity Add(TEntity entityToAdd)
        {
            if (entityToAdd == null)
                throw new ArgumentNullException(nameof(entityToAdd));

            if (entityToAdd is IEntityWithDefaultGuidKey entityToAddAsEntityWithDefaultGuidKey && entityToAddAsEntityWithDefaultGuidKey.Id == Guid.Empty)
                entityToAddAsEntityWithDefaultGuidKey.Id = Guid.NewGuid();
            if (entityToAdd is IVersionableEntity versionableEntity)
                versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

            DbContext.Add(entityToAdd);

            SaveChanges();

            return entityToAdd;
        }

        public virtual IEnumerable<TEntity> AddRange(IEnumerable<TEntity> entitiesToAdd)
        {
            if (entitiesToAdd == null)
                throw new ArgumentNullException(nameof(entitiesToAdd));

            List<TEntity> entityToAddList = entitiesToAdd as List<TEntity> ?? entitiesToAdd.ToList();

            foreach (TEntity entityToAdd in entityToAddList)
            {
                if (entityToAdd is IEntityWithDefaultGuidKey entityToAddAsEntityWithDefaultGuidKey && entityToAddAsEntityWithDefaultGuidKey.Id == Guid.Empty)
                    entityToAddAsEntityWithDefaultGuidKey.Id = Guid.NewGuid();
                if (entityToAdd is IVersionableEntity versionableEntity)
                    versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;
            }

            DbContext.AddRange(entityToAddList);

            SaveChanges();

            return entityToAddList;
        }

        public virtual TEntity Update(TEntity entityToUpdate)
        {
            if (entityToUpdate == null)
                throw new ArgumentNullException(nameof(entityToUpdate));

            if (entityToUpdate is IVersionableEntity versionableEntity)
                versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

            Attach(entityToUpdate);
            DbContext.Entry(entityToUpdate).State = EntityState.Modified;

            SaveChanges();

            Detach(entityToUpdate);

            return entityToUpdate;
        }

        public virtual TEntity Delete(TEntity entityToDelete)
        {
            if (entityToDelete == null)
                throw new ArgumentNullException(nameof(entityToDelete));

            if (entityToDelete is IVersionableEntity versionableEntity)
                versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

            if (entityToDelete is IArchivableEntity archivableEntity)
            {
                archivableEntity.IsArchived = true;
                return Update(entityToDelete);
            }
            else
            {
                DbContext.Remove(entityToDelete);
                SaveChanges();
                return entityToDelete;
            }
        }

        public virtual IQueryable<TEntity> GetAll()
        {
            return Set.AsNoTracking();
        }

        public virtual Task<IQueryable<TEntity>> GetAllAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Set.AsNoTracking());
        }

        public virtual IQueryable<TChild> GetCollectionQuery<TChild>(TEntity entity, Expression<Func<TEntity, IEnumerable<TChild>>> childs) where TChild : class
        {
            Attach(entity);

            return DbContext.Entry(entity).Collection(childs).Query();
        }

        public virtual async Task LoadCollectionAsync<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> childs, CancellationToken cancellationToken) where TProperty : class
        {
            Attach(entity);

            CollectionEntry<TEntity, TProperty> collection = DbContext.Entry(entity).Collection(childs);

            if (collection.IsLoaded == false)
                await collection.LoadAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual void LoadCollection<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> childs)
            where TProperty : class
        {
            Attach(entity);

            CollectionEntry<TEntity, TProperty> collection = DbContext.Entry(entity).Collection(childs);

            if (collection.IsLoaded == false)
                collection.Load();
        }

        public virtual async Task LoadReferenceAsync<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> member, CancellationToken cancellationToken)
            where TProperty : class
        {
            Attach(entity);

            ReferenceEntry<TEntity, TProperty> reference = DbContext.Entry(entity).Reference(member);

            if (reference.IsLoaded == false)
                await reference.LoadAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual void LoadReference<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> member)
            where TProperty : class
        {
            Attach(entity);

            ReferenceEntry<TEntity, TProperty> reference = DbContext.Entry(entity).Reference(member);

            if (reference.IsLoaded == false)
                reference.Load();
        }

        public virtual Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            DbContext.ChangeTracker.DetectChanges();
            return DbContext.SaveChangesAsync(cancellationToken);
        }

        public virtual void SaveChanges()
        {
            DbContext.ChangeTracker.DetectChanges();
            DbContext.SaveChanges();
        }

        public virtual async Task<TEntity> GetByIdAsync(CancellationToken cancellationToken, params object[] ids)
        {
            return await EfDataProviderSpecificMethodsProvider.ApplyWhereByKeys((await GetAllAsync(CancellationToken.None).ConfigureAwait(false)), ids)
                .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual TEntity GetById(params object[] ids)
        {
            return EfDataProviderSpecificMethodsProvider.ApplyWhereByKeys(GetAll(), ids)
                .SingleOrDefault();
        }

        public virtual EfCoreDataProviderSpecificMethodsProvider EfDataProviderSpecificMethodsProvider { get; set; }

        public virtual IDateTimeProvider DateTimeProvider { get; set; }
    }
}