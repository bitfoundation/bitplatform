using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Foundation.Model.Contracts;
using System.Linq;

namespace Foundation.DataAccess.Contracts
{
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

        IQueryable<TChild> GetChildsQuery<TChild>(TEntity entity, Expression<Func<TEntity, ICollection<TChild>>> childs)
            where TChild : class;

        Task LoadChildsAsync<TProperty>(TEntity entity, Expression<Func<TEntity, ICollection<TProperty>>> member,
            CancellationToken cancellationToken, bool forceReload = false)
            where TProperty : class;

        Task LoadMemberAsync<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> member,
            CancellationToken cancellationToken, bool forceReload = false)
            where TProperty : class;

        Task<bool> AnyChildAsync<TChild>(TEntity entity, Expression<Func<TEntity, ICollection<TChild>>> childs,
            Expression<Func<TChild, bool>> predicate, bool checkDatabase, CancellationToken cancellationToken)
            where TChild : class;

        bool AnyChild<TChild>(TEntity entity, Expression<Func<TEntity, ICollection<TChild>>> childs,
            Expression<Func<TChild, bool>> predicate, bool checkDatabase)
            where TChild : class;
    }
}