using Bit.Core.Contracts;
using Bit.Data.Contracts;
using Bit.Model.Contracts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Data.EntityFramework.Implementations
{
    /// <summary>
    /// Entity Framework implementation of <see cref="Bit.Data.Contracts.IRepository{TEntity}"/> which uses <see cref="Bit.Data.EntityFramework.Implementations.EfDbContextBase"/>
    /// </summary>
    /// <typeparam name="TEntity">Entity class with <see cref="Bit.Model.Contracts.IEntity"/> marker</typeparam>
    public class EfRepository<TDbContext, TEntity> : IRepository<TEntity>
        where TEntity : class, IEntity
        where TDbContext : DbContext
    {
        private TDbContext _dbContext;

        public virtual TDbContext DbContext
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

            try
            {
                if (entityToAdd is IEntityWithDefaultGuidKey entityToAddAsEntityWithDefaultGuidKey && entityToAddAsEntityWithDefaultGuidKey.Id == Guid.Empty)
                    entityToAddAsEntityWithDefaultGuidKey.Id = Guid.NewGuid();
                if (entityToAdd is IVersionableEntity versionableEntity)
                    versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

                if (entityToAdd is ISyncableEntity syncableEntity)
                {
                    ObjectContext objectContext = ((IObjectContextAdapter)DbContext).ObjectContext;
                    ObjectSet<TEntity> set = objectContext.CreateObjectSet<TEntity>();
                    object[] keys = set.EntitySet.ElementType
                        .KeyMembers
                        .Select(k => typeof(TEntity).GetTypeInfo().GetProperty(k.Name).GetValue(syncableEntity))
                        .ToArray();

                    TEntity entityIfExists = await GetByIdAsync(cancellationToken, keys).ConfigureAwait(false);

                    if (entityIfExists != null)
                        return entityIfExists;
                }

                Set.Add(entityToAdd);

                await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                return entityToAdd;
            }
            finally
            {
                if (DbContext is EfDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(entityToAdd);
            }
        }

        public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entitiesToAdd, CancellationToken cancellationToken)
        {
            if (entitiesToAdd == null)
                throw new ArgumentNullException(nameof(entitiesToAdd));

            List<TEntity> entitiesToAddList = entitiesToAdd as List<TEntity> ?? entitiesToAdd.ToList();

            try
            {
                foreach (TEntity entityToAdd in entitiesToAddList)
                {
                    if (entityToAdd is IEntityWithDefaultGuidKey entityToAddAsEntityWithDefaultGuidKey && entityToAddAsEntityWithDefaultGuidKey.Id == Guid.Empty)
                        entityToAddAsEntityWithDefaultGuidKey.Id = Guid.NewGuid();
                    if (entityToAdd is IVersionableEntity versionableEntity)
                        versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;
                }

                Set.AddRange(entitiesToAddList);

                await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                return entitiesToAddList;
            }
            finally
            {
                if (DbContext is EfDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    entitiesToAddList.ForEach(Detach);
            }
        }

        public virtual async Task<TEntity> UpdateAsync(TEntity entityToUpdate, CancellationToken cancellationToken)
        {
            if (entityToUpdate == null)
                throw new ArgumentNullException(nameof(entityToUpdate));

            try
            {
                if (entityToUpdate is IVersionableEntity versionableEntity)
                    versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

                Attach(entityToUpdate);
                DbContext.Entry(entityToUpdate).State = EntityState.Modified;

                await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                return entityToUpdate;
            }
            finally
            {
                if (DbContext is EfDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(entityToUpdate);
            }
        }

        public virtual async Task<TEntity> DeleteAsync(TEntity entityToDelete, CancellationToken cancellationToken)
        {
            if (entityToDelete == null)
                throw new ArgumentNullException(nameof(entityToDelete));

            try
            {
                if (entityToDelete is IVersionableEntity versionableEntity)
                    versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

                if (entityToDelete is IArchivableEntity archivableEntity)
                {
                    archivableEntity.IsArchived = true;
                    return await UpdateAsync(entityToDelete, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    Attach(entityToDelete);
                    DbContext.Entry(entityToDelete).State = EntityState.Deleted;
                    await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    return entityToDelete;
                }
            }
            finally
            {
                if (DbContext is EfDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(entityToDelete);
            }
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

            try
            {
                if (entityToAdd is IEntityWithDefaultGuidKey entityToAddAsEntityWithDefaultGuidKey && entityToAddAsEntityWithDefaultGuidKey.Id == Guid.Empty)
                    entityToAddAsEntityWithDefaultGuidKey.Id = Guid.NewGuid();
                if (entityToAdd is IVersionableEntity versionableEntity)
                    versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

                Set.Add(entityToAdd);

                SaveChanges();

                return entityToAdd;
            }
            finally
            {
                if (DbContext is EfDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(entityToAdd);
            }
        }

        public virtual IEnumerable<TEntity> AddRange(IEnumerable<TEntity> entitiesToAdd)
        {
            if (entitiesToAdd == null)
                throw new ArgumentNullException(nameof(entitiesToAdd));

            List<TEntity> entitiesToAddList = entitiesToAdd as List<TEntity> ?? entitiesToAdd.ToList();

            try
            {
                foreach (TEntity entityToAdd in entitiesToAddList)
                {
                    if (entityToAdd is IEntityWithDefaultGuidKey entityToAddAsEntityWithDefaultGuidKey && entityToAddAsEntityWithDefaultGuidKey.Id == Guid.Empty)
                        entityToAddAsEntityWithDefaultGuidKey.Id = Guid.NewGuid();
                    if (entityToAdd is IVersionableEntity versionableEntity)
                        versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;
                }

                Set.AddRange(entitiesToAddList);

                SaveChanges();

                return entitiesToAddList;
            }
            finally
            {
                if (DbContext is EfDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    entitiesToAddList.ForEach(Detach);
            }
        }

        public virtual TEntity Update(TEntity entityToUpdate)
        {
            if (entityToUpdate == null)
                throw new ArgumentNullException(nameof(entityToUpdate));

            try
            {
                if (entityToUpdate is IVersionableEntity versionableEntity)
                    versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

                Attach(entityToUpdate);
                DbContext.Entry(entityToUpdate).State = EntityState.Modified;

                SaveChanges();

                return entityToUpdate;
            }
            finally
            {
                if (DbContext is EfDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(entityToUpdate);
            }
        }

        public virtual TEntity Delete(TEntity entityToDelete)
        {
            if (entityToDelete == null)
                throw new ArgumentNullException(nameof(entityToDelete));

            try
            {
                if (entityToDelete is IVersionableEntity versionableEntity)
                    versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

                if (entityToDelete is IArchivableEntity archivableEntity)
                {
                    archivableEntity.IsArchived = true;
                    return Update(entityToDelete);
                }
                else
                {
                    Attach(entityToDelete);
                    DbContext.Entry(entityToDelete).State = EntityState.Deleted;
                    SaveChanges();
                    return entityToDelete;
                }
            }
            finally
            {
                if (DbContext is EfDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(entityToDelete);
            }
        }

        public virtual IQueryable<TEntity> GetAll()
        {
            if (DbContext is EfDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                return Set.AsNoTracking();
            else
                return Set;
        }

        public virtual Task<IQueryable<TEntity>> GetAllAsync(CancellationToken cancellationToken)
        {
            if (DbContext is EfDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                return Task.FromResult((IQueryable<TEntity>)Set.AsNoTracking());
            else
                return Task.FromResult((IQueryable<TEntity>)Set);
        }

        public virtual async Task LoadCollectionAsync<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> childs, CancellationToken cancellationToken)
            where TProperty : class
        {
            try
            {
                Expression<Func<TEntity, ICollection<TProperty>>> convertedChilds = Expression.Lambda<Func<TEntity, ICollection<TProperty>>>(childs.Body, childs.Parameters);

                Attach(entity);

                DbCollectionEntry<TEntity, TProperty> collection = DbContext.Entry(entity).Collection(convertedChilds);

                if (collection.IsLoaded == false)
                    await collection.LoadAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (DbContext is EfDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(entity);
            }
        }

        public virtual void LoadCollection<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> childs)
            where TProperty : class
        {
            try
            {
                Expression<Func<TEntity, ICollection<TProperty>>> convertedChilds = Expression.Lambda<Func<TEntity, ICollection<TProperty>>>(childs.Body, childs.Parameters);

                Attach(entity);

                DbCollectionEntry<TEntity, TProperty> collection = DbContext.Entry(entity).Collection(convertedChilds);

                if (collection.IsLoaded == false)
                    collection.Load();
            }
            finally
            {
                if (DbContext is EfDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(entity);
            }
        }

        public virtual async Task LoadReferenceAsync<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> member, CancellationToken cancellationToken)
            where TProperty : class
        {
            try
            {
                Attach(entity);

                DbReferenceEntry<TEntity, TProperty> reference = DbContext.Entry(entity).Reference(member);

                if (reference.IsLoaded == false)
                    await reference.LoadAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (DbContext is EfDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(entity);
            }
        }

        public virtual void LoadReference<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> member)
            where TProperty : class
        {
            try
            {
                Attach(entity);

                DbReferenceEntry<TEntity, TProperty> reference = DbContext.Entry(entity).Reference(member);

                if (reference.IsLoaded == false)
                    reference.Load();
            }
            finally
            {
                if (DbContext is EfDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(entity);
            }
        }

        /// <summary>
        /// Unit of work is being handled by implicit unit of work implementation. SaveChanges is a non public method which is not present in Repository contract.
        /// </summary>
        protected virtual Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            DbContext.ChangeTracker.DetectChanges();
            return DbContext.SaveChangesAsync(cancellationToken);
        }

        protected virtual void SaveChanges()
        {
            DbContext.ChangeTracker.DetectChanges();
            DbContext.SaveChanges();
        }

        public virtual async Task<TEntity> GetByIdAsync(CancellationToken cancellationToken, params object[] keys)
        {
            return await EfDataProviderSpecificMethodsProvider.ApplyWhereByKeys((await GetAllAsync(cancellationToken).ConfigureAwait(false)), keys)
                .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual TEntity GetById(params object[] keys)
        {
            return EfDataProviderSpecificMethodsProvider.ApplyWhereByKeys(GetAll(), keys)
                .SingleOrDefault();
        }

        public virtual async Task ReloadAsync(TEntity entity, CancellationToken cancellationToken)
        {
            try
            {
                Attach(entity);

                await DbContext.Entry(entity).ReloadAsync(cancellationToken);
            }
            finally
            {
                if (DbContext is EfDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(entity);
            }
        }

        public virtual void Reload(TEntity entity)
        {
            try
            {
                Attach(entity);

                DbContext.Entry(entity).Reload();
            }
            finally
            {
                if (DbContext is EfDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                    Detach(entity);
            }
        }

        public virtual IQueryable<TChild> GetCollectionQuery<TChild>(TEntity entity, Expression<Func<TEntity, IEnumerable<TChild>>> childs) where TChild : class
        {
            if (DbContext is EfDbContextBase dbContextBase && dbContextBase.ChangeTrackingEnabled() == false)
                throw new InvalidOperationException("This operation is valid for db context with change tracking enabled");

            Attach(entity);

            Expression<Func<TEntity, ICollection<TChild>>> convertedChilds = Expression.Lambda<Func<TEntity, ICollection<TChild>>>(childs.Body, childs.Parameters);

            return DbContext.Entry(entity).Collection(convertedChilds).Query();
        }

        public virtual EfDataProviderSpecificMethodsProvider EfDataProviderSpecificMethodsProvider { get; set; }

        public virtual IDateTimeProvider DateTimeProvider { get; set; }
    }

    public class EfRepository<TEntity> : EfRepository<EfDbContextBase, TEntity>, IRepository<TEntity>
        where TEntity : class, IEntity
    {

    }
}
