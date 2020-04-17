using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Data.Contracts;
using Bit.Model.Contracts;
using NHibernate;

namespace Bit.Data.NHibernate.Implementations
{
    public class NHRepository<TEntity> : IRepository<TEntity>
         where TEntity : class, IEntity
    {
        public virtual ISession Session { get; set; } = default!;

        public virtual IDateTimeProvider DateTimeProvider { get; set; } = default!;

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
                TEntity? entityIfExists = await GetByIdAsync(cancellationToken, GetEntityId(syncableEntity)).ConfigureAwait(false);

                if (entityIfExists != null)
                    return entityIfExists;
            }

            await Session.SaveAsync(entityToAdd, cancellationToken).ConfigureAwait(false);
            await Session.FlushAsync(cancellationToken).ConfigureAwait(false);

            return entityToAdd;
        }

        public virtual TEntity Add(TEntity entityToAdd)
        {
            if (entityToAdd == null)
                throw new ArgumentNullException(nameof(entityToAdd));

            if (entityToAdd is IEntityWithDefaultGuidKey entityToAddAsEntityWithDefaultGuidKey && entityToAddAsEntityWithDefaultGuidKey.Id == Guid.Empty)
                entityToAddAsEntityWithDefaultGuidKey.Id = Guid.NewGuid();
            if (entityToAdd is IVersionableEntity versionableEntity)
                versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

            if (entityToAdd is ISyncableEntity syncableEntity)
            {
                TEntity? entityIfExists = GetById(GetEntityId(syncableEntity));

                if (entityIfExists != null)
                    return entityIfExists;
            }

            Session.Save(entityToAdd);
            Session.Flush();

            return entityToAdd;
        }

        public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entitiesToAdd, CancellationToken cancellationToken)
        {
            if (entitiesToAdd == null)
                throw new ArgumentNullException(nameof(entitiesToAdd));

            List<TEntity> addedEntities = new List<TEntity>();

            foreach (TEntity entityToAdd in entitiesToAdd)
            {
                addedEntities.Add(await AddAsync(entityToAdd, cancellationToken).ConfigureAwait(false));
            }

            return addedEntities;
        }

        public virtual IEnumerable<TEntity> AddRange(IEnumerable<TEntity> entitiesToAdd)
        {
            if (entitiesToAdd == null)
                throw new ArgumentNullException(nameof(entitiesToAdd));

            List<TEntity> addedEntities = new List<TEntity>();

            foreach (TEntity entityToAdd in entitiesToAdd)
            {
                addedEntities.Add(Add(entityToAdd));
            }

            return addedEntities;
        }

        public virtual async Task<TEntity> UpdateAsync(TEntity entityToUpdate, CancellationToken cancellationToken)
        {
            if (entityToUpdate == null)
                throw new ArgumentNullException(nameof(entityToUpdate));

            if (entityToUpdate is IVersionableEntity versionableEntity)
                versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

            await Session.UpdateAsync(entityToUpdate, cancellationToken).ConfigureAwait(false);
            await Session.FlushAsync(cancellationToken).ConfigureAwait(false);

            return entityToUpdate;
        }

        public virtual TEntity Update(TEntity entityToUpdate)
        {
            if (entityToUpdate == null)
                throw new ArgumentNullException(nameof(entityToUpdate));

            if (entityToUpdate is IVersionableEntity versionableEntity)
                versionableEntity.Version = DateTimeProvider.GetCurrentUtcDateTime().UtcTicks;

            Session.Update(entityToUpdate);
            Session.Flush();

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
                await Session.DeleteAsync(entityToDelete, cancellationToken).ConfigureAwait(false);
                await Session.FlushAsync(cancellationToken).ConfigureAwait(false);

                return entityToDelete;
            }
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
                Session.Delete(entityToDelete);
                Session.Flush();

                return entityToDelete;
            }
        }

        public virtual IQueryable<TEntity> GetAll()
        {
            return Session.Query<TEntity>();
        }

        public virtual Task<IQueryable<TEntity>> GetAllAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Session.Query<TEntity>());
        }

        public virtual void LoadCollection<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> childs)
            where TProperty : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (childs == null)
                throw new ArgumentNullException(nameof(childs));

            childs.Compile().Invoke(entity);
        }

        public virtual Task LoadCollectionAsync<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> childs, CancellationToken cancellationToken)
            where TProperty : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (childs == null)
                throw new ArgumentNullException(nameof(childs));

            childs.Compile().Invoke(entity);
            return Task.CompletedTask;
        }

        public virtual Task LoadReferenceAsync<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> member, CancellationToken cancellationToken)
            where TProperty : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (member == null)
                throw new ArgumentNullException(nameof(member));

            member.Compile().Invoke(entity);
            return Task.CompletedTask;
        }

        public virtual void LoadReference<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> member)
            where TProperty : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (member == null)
                throw new ArgumentNullException(nameof(member));

            member.Compile().Invoke(entity);
        }

        public virtual Task<TEntity?> GetByIdAsync(CancellationToken cancellationToken, params object[] ids)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            return Session.GetAsync<TEntity?>(ids.ExtendedSingle("Getting id"), cancellationToken);
        }

        public virtual TEntity? GetById(params object[] ids)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            return Session.Get<TEntity>(ids.ExtendedSingle("Getting id"));
        }

        public virtual Task ReloadAsync(TEntity entity, CancellationToken cancellationToken)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return Session.RefreshAsync(entity, cancellationToken);
        }

        public virtual void Reload(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            Session.Refresh(entity);
        }

        public virtual IQueryable<TChild> GetCollectionQuery<TChild>(TEntity entity, Expression<Func<TEntity, IEnumerable<TChild>>> childs)
            where TChild : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (childs == null)
                throw new ArgumentNullException(nameof(childs));

            throw new NotImplementedException();
        }

        object GetEntityId(object entity)
        {
            string idPropName = Session.SessionFactory.GetClassMetadata(typeof(TEntity)).IdentifierPropertyName;
            return (typeof(TEntity).GetProperty(idPropName)!).GetValue(entity) ?? throw new InvalidOperationException($"Member {idPropName} may not be null");
        }
    }
}
