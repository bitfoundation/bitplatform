using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Foundation.DataAccess.Contracts;
using Foundation.Model.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Foundation.DataAccess.Implementations.EntityFrameworkCore
{
    public class EfRepository<TEntity> : IRepository<TEntity>
        where TEntity : class, IEntity
    {
        private readonly DefaultDbContext _dbContext;
        private readonly DbSet<TEntity> _set;

        protected EfRepository()
        {
        }

        protected EfRepository(DefaultDbContext dbContext)
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

            _dbContext.Add(entityToAdd);

            await SaveChangesAsync(cancellationToken);

            return entityToAdd;
        }

        public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entitiesToAdd, CancellationToken cancellationToken)
        {
            if (entitiesToAdd == null)
                throw new ArgumentNullException(nameof(entitiesToAdd));

            _dbContext.AddRange(entitiesToAdd);

            await SaveChangesAsync(cancellationToken);

            return entitiesToAdd;
        }

        public virtual async Task<TEntity> UpdateAsync(TEntity entityToUpdate, CancellationToken cancellationToken)
        {
            if (entityToUpdate == null)
                throw new ArgumentNullException(nameof(entityToUpdate));

            _dbContext.Update(entityToUpdate);

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
                _dbContext.Remove(entityToDelete);
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

            _dbContext.Attach(entity);
        }

        public virtual TEntity Add(TEntity entityToAdd)
        {
            if (entityToAdd == null)
                throw new ArgumentNullException(nameof(entityToAdd));

            _dbContext.Add(entityToAdd);

            SaveChanges();

            return entityToAdd;
        }

        public virtual IEnumerable<TEntity> AddRange(IEnumerable<TEntity> entitiesToAdd)
        {
            if (entitiesToAdd == null)
                throw new ArgumentNullException(nameof(entitiesToAdd));

            _dbContext.AddRange(entitiesToAdd);

            SaveChanges();

            return entitiesToAdd;
        }

        public virtual TEntity Update(TEntity entityToUpdate)
        {
            if (entityToUpdate == null)
                throw new ArgumentNullException(nameof(entityToUpdate));

            _dbContext.Update(entityToUpdate);

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
                _dbContext.Remove(entityToDelete);
                SaveChanges();
                return entityToDelete;
            }
        }

        public virtual IQueryable<TEntity> GetAll()
        {
            return _set.AsNoTracking();
        }

        public virtual IQueryable<TChild> GetChildsQuery<TChild>(TEntity entity, Expression<Func<TEntity, ICollection<TChild>>> childs) where TChild : class
        {
            throw new NotImplementedException();
        }

        public virtual Task LoadChildsAsync<TProperty>(TEntity entity, Expression<Func<TEntity, ICollection<TProperty>>> member, CancellationToken cancellationToken, bool forceReload = false) where TProperty : class
        {
            throw new NotImplementedException();
        }

        public virtual Task LoadMemberAsync<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> member, CancellationToken cancellationToken, bool forceReload = false) where TProperty : class
        {
            throw new NotImplementedException();
        }

        public virtual Task<bool> AnyChildAsync<TChild>(TEntity entity, Expression<Func<TEntity, ICollection<TChild>>> childs, Expression<Func<TChild, bool>> predicate, bool checkDatabase, CancellationToken cancellationToken) where TChild : class
        {
            throw new NotImplementedException();
        }

        public virtual bool AnyChild<TChild>(TEntity entity, Expression<Func<TEntity, ICollection<TChild>>> childs, Expression<Func<TChild, bool>> predicate, bool checkDatabase) where TChild : class
        {
            throw new NotImplementedException();
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
    }
}