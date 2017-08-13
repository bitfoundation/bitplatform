using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Bit.Model.Contracts;

namespace Bit.Data.Contracts
{
    /// <summary>
    /// Contract of Bit repositories
    /// </summary>
    /// <typeparam name="TEntity">Entity class with <see cref="Bit.Model.Contracts.IEntity"/> marker</typeparam>
    public interface IRepository<TEntity>
        where TEntity : class, IEntity
    {
        Task<TEntity> AddAsync(TEntity entityToAdd, CancellationToken cancellationToken);

        TEntity Add(TEntity entityToAdd);

        Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entitiesToAdd, CancellationToken cancellationToken);

        IEnumerable<TEntity> AddRange(IEnumerable<TEntity> entitiesToAdd);

        Task<TEntity> UpdateAsync(TEntity entityToUpdate, CancellationToken cancellationToken);

        TEntity Update(TEntity entityToUpdate);

        Task<TEntity> DeleteAsync(TEntity entityToDelete, CancellationToken cancellationToken);

        TEntity Delete(TEntity entityToDelete);

        bool IsChangedProperty<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> prop);

        TProperty GetOriginalValue<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> prop);

        bool IsDeleted(TEntity entity);

        bool IsAdded(TEntity entity);

        bool IsModified(TEntity entity);

        void Detach(TEntity entity);

        void Attach(TEntity entity);

        IQueryable<TEntity> GetAll();

        Task<IQueryable<TEntity>> GetAllAsync(CancellationToken cancellationToken);

        IQueryable<TChild> GetCollectionQuery<TChild>(TEntity entity, Expression<Func<TEntity, IEnumerable<TChild>>> childs)
            where TChild : class;

        void LoadCollection<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> childs, bool forceReload = false)
            where TProperty : class;

        Task LoadCollectionAsync<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> childs,
            CancellationToken cancellationToken, bool forceReload = false)
            where TProperty : class;

        Task LoadReferenceAsync<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> member,
            CancellationToken cancellationToken, bool forceReload = false)
            where TProperty : class;

        void LoadReference<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> member, bool forceReload = false)
            where TProperty : class;

        Task SaveChangesAsync(CancellationToken cancellationToken);

        void SaveChanges();

        Task<TEntity> GetByIdAsync(params object[] ids);

        TEntity GetById(params object[] ids);
    }
}