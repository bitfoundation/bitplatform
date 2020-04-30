using Bit.Core.Contracts;
using Bit.Data.Contracts;
using Bit.Model.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Data.EntityFrameworkCore.Implementations
{
    public class EfCoreRepositoryBase<TDbContext, T> : IRepository<T>
        where T : class
        where TDbContext : DbContext
    {
        private TDbContext _dbContext = default!;
        public virtual TDbContext DbContext
        {
            get => _dbContext;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _dbContext = value;
                _set = _dbContext.Set<T>();
            }
        }

        private DbSet<T> _set = default!;

        protected virtual DbSet<T> Set => _set;

        public virtual async Task<T> AddAsync(T itemToAdd, CancellationToken cancellationToken)
        {
            if (itemToAdd == null)
                throw new ArgumentNullException(nameof(itemToAdd));

            if (itemToAdd is IEntityWithDefaultGuidKey entityToAddAsEntityWithDefaultGuidKey && entityToAddAsEntityWithDefaultGuidKey.Id == Guid.Empty)
                entityToAddAsEntityWithDefaultGuidKey.Id = Guid.NewGuid();
            if (itemToAdd is IVersionableEntity versionableEntity)
                versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;
            if (itemToAdd is IVersionableDto versionableDto)
                versionableDto.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

            if (itemToAdd is ISyncableEntity syncableEntity)
            {
                object[] keys = DbContext.Model.FindEntityType(typeof(T).GetTypeInfo())
                    .FindPrimaryKey()
                    .Properties
                    .Select(p => p.PropertyInfo.GetValue(syncableEntity)!)
                    .ToArray();

                T? entityIfExists = await GetByIdAsync(cancellationToken, keys).ConfigureAwait(false);

                if (entityIfExists != null)
                    return entityIfExists;
            }

            await DbContext.AddAsync(itemToAdd, cancellationToken).ConfigureAwait(false);

            await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return itemToAdd;
        }

        public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entitiesToAdd, CancellationToken cancellationToken)
        {
            if (entitiesToAdd == null)
                throw new ArgumentNullException(nameof(entitiesToAdd));

            List<T> itemsToAdd = entitiesToAdd as List<T> ?? entitiesToAdd.ToList();

            foreach (T itemToAdd in itemsToAdd)
            {
                if (itemToAdd is IEntityWithDefaultGuidKey entityToAddAsEntityWithDefaultGuidKey && entityToAddAsEntityWithDefaultGuidKey.Id == Guid.Empty)
                    entityToAddAsEntityWithDefaultGuidKey.Id = Guid.NewGuid();
                if (itemToAdd is IVersionableEntity versionableEntity)
                    versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;
                if (itemToAdd is IVersionableDto versionableDto)
                    versionableDto.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;
            }

            await DbContext.AddRangeAsync(itemsToAdd, cancellationToken).ConfigureAwait(false);

            await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return itemsToAdd;
        }

        public virtual async Task<T> UpdateAsync(T itemToUpdate, CancellationToken cancellationToken)
        {
            if (itemToUpdate == null)
                throw new ArgumentNullException(nameof(itemToUpdate));

            if (itemToUpdate is IVersionableEntity versionableEntity)
                versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;
            if (itemToUpdate is IVersionableDto versionableDto)
                versionableDto.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

            DbContext.Update(itemToUpdate);

            await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return itemToUpdate;
        }

        public virtual async Task<T> DeleteAsync(T itemToDelete, CancellationToken cancellationToken)
        {
            if (itemToDelete == null)
                throw new ArgumentNullException(nameof(itemToDelete));

            if (itemToDelete is IVersionableEntity versionableEntity)
                versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;
            if (itemToDelete is IVersionableDto versionableDto)
                versionableDto.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

            if (itemToDelete is IArchivableEntity archivableEntity)
            {
                archivableEntity.IsArchived = true;
                return await UpdateAsync(itemToDelete, cancellationToken).ConfigureAwait(false);
            }
            else if (itemToDelete is IArchivableDto archivableDto && !(itemToDelete is ISyncableDto /*SyncableDto items are being handled in DbContext's SaveChanges*/))
            {
                archivableDto.IsArchived = true;
                return await UpdateAsync(itemToDelete, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                DbContext.Remove(itemToDelete);

                await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                return itemToDelete;
            }
        }

        public virtual void Detach(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            Attach(item);

            DbContext.Entry(item).State = EntityState.Detached;
        }

        public virtual void Attach(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (DbContext.Entry(item).State == EntityState.Detached)
                Set.Attach(item);
        }

        public virtual T Add(T itemToAdd)
        {
            if (itemToAdd == null)
                throw new ArgumentNullException(nameof(itemToAdd));

            if (itemToAdd is IEntityWithDefaultGuidKey entityToAddAsEntityWithDefaultGuidKey && entityToAddAsEntityWithDefaultGuidKey.Id == Guid.Empty)
                entityToAddAsEntityWithDefaultGuidKey.Id = Guid.NewGuid();
            if (itemToAdd is IVersionableEntity versionableEntity)
                versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;
            if (itemToAdd is IVersionableDto versionableDto)
                versionableDto.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

            DbContext.Add(itemToAdd);

            SaveChanges();

            return itemToAdd;
        }

        public virtual IEnumerable<T> AddRange(IEnumerable<T> entitiesToAdd)
        {
            if (entitiesToAdd == null)
                throw new ArgumentNullException(nameof(entitiesToAdd));

            List<T> itemsToAdd = entitiesToAdd as List<T> ?? entitiesToAdd.ToList();

            foreach (T itemToAdd in itemsToAdd)
            {
                if (itemToAdd is IEntityWithDefaultGuidKey entityToAddAsEntityWithDefaultGuidKey && entityToAddAsEntityWithDefaultGuidKey.Id == Guid.Empty)
                    entityToAddAsEntityWithDefaultGuidKey.Id = Guid.NewGuid();
                if (itemToAdd is IVersionableEntity versionableEntity)
                    versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;
                if (itemToAdd is IVersionableDto versionableDto)
                    versionableDto.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;
            }

            DbContext.AddRange(itemsToAdd);

            SaveChanges();

            return itemsToAdd;
        }

        public virtual T Update(T itemToUpdate)
        {
            if (itemToUpdate == null)
                throw new ArgumentNullException(nameof(itemToUpdate));

            if (itemToUpdate is IVersionableEntity versionableEntity)
                versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;
            if (itemToUpdate is IVersionableDto versionableDto)
                versionableDto.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

            DbContext.Update(itemToUpdate);

            SaveChanges();

            return itemToUpdate;
        }

        public virtual T Delete(T itemToDelete)
        {
            if (itemToDelete == null)
                throw new ArgumentNullException(nameof(itemToDelete));

            if (itemToDelete is IVersionableEntity versionableEntity)
                versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;
            if (itemToDelete is IVersionableDto versionableDto)
                versionableDto.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

            if (itemToDelete is IArchivableEntity archivableEntity)
            {
                archivableEntity.IsArchived = true;
                return Update(itemToDelete);
            }
            else if (itemToDelete is IArchivableDto archivableDto && !(itemToDelete is ISyncableDto /*SyncableDto items are being handled in DbContext's SaveChanges*/))
            {
                archivableDto.IsArchived = true;
                return Update(itemToDelete);
            }
            else
            {
                DbContext.Remove(itemToDelete);

                SaveChanges();

                return itemToDelete;
            }
        }

        public virtual IQueryable<T> GetAll()
        {
            return Set;
        }

        public virtual Task<IQueryable<T>> GetAllAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult((IQueryable<T>)Set);
        }

        public virtual async Task LoadCollectionAsync<TProperty>(T item, Expression<Func<T, IEnumerable<TProperty?>>> childs, CancellationToken cancellationToken)
            where TProperty : class
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (childs == null)
                throw new ArgumentNullException(nameof(childs));

            Attach(item);

            CollectionEntry<T, TProperty?> collection = DbContext.Entry(item).Collection(childs);

            if (collection.IsLoaded == false)
                await collection.LoadAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual void LoadCollection<TProperty>(T item, Expression<Func<T, IEnumerable<TProperty?>>> childs)
            where TProperty : class
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (childs == null)
                throw new ArgumentNullException(nameof(childs));

            Attach(item);

            CollectionEntry<T, TProperty?> collection = DbContext.Entry(item).Collection(childs);

            if (collection.IsLoaded == false)
                collection.Load();
        }

        public virtual async Task LoadReferenceAsync<TProperty>(T item, Expression<Func<T, TProperty?>> member, CancellationToken cancellationToken)
            where TProperty : class
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (member == null)
                throw new ArgumentNullException(nameof(member));

            Attach(item);

            ReferenceEntry<T, TProperty?> reference = DbContext.Entry(item).Reference(member);

            if (reference.IsLoaded == false)
                await reference.LoadAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual void LoadReference<TProperty>(T item, Expression<Func<T, TProperty?>> member)
            where TProperty : class
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (member == null)
                throw new ArgumentNullException(nameof(member));

            Attach(item);

            ReferenceEntry<T, TProperty?> reference = DbContext.Entry(item).Reference(member);

            if (reference.IsLoaded == false)
                reference.Load();
        }

        /// <summary>
        /// Unit of work is being handled by implicit unit of work implementation. SaveChanges is a non public method which is not present in Repository contract.
        /// </summary>
        protected virtual Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return DbContext.SaveChangesAsync(cancellationToken);
        }

        protected virtual void SaveChanges()
        {
            DbContext.SaveChanges();
        }

        public virtual async Task<T?> GetByIdAsync(CancellationToken cancellationToken, params object[] ids)
        {
            return await Set.FindAsync(ids, cancellationToken);
        }

        public virtual T? GetById(params object[] ids)
        {
            return Set.Find(ids);
        }

        public virtual async Task ReloadAsync(T item, CancellationToken cancellationToken)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            Attach(item);

            await DbContext.Entry(item).ReloadAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual void Reload(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            Attach(item);

            DbContext.Entry(item).Reload();
        }

        public virtual IQueryable<TChild> GetCollectionQuery<TChild>(T item, Expression<Func<T, IEnumerable<TChild>>> childs) where TChild : class
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (childs == null)
                throw new ArgumentNullException(nameof(childs));

            Attach(item);

            return DbContext.Entry(item).Collection(childs).Query();
        }

        public virtual IDateTimeProvider DateTimeProvider { get; set; } = default!;
    }

    public class EfCoreRepositoryBase<TEntity> : EfCoreRepositoryBase<EfCoreDbContextBase, TEntity>, IRepository<TEntity>
        where TEntity : class, IEntity
    {

    }
}
