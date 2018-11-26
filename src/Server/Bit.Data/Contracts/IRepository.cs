using Bit.Model.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

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

        IQueryable<TEntity> GetAll();

        Task<IQueryable<TEntity>> GetAllAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Loads the collection of <paramref name="childs"/> entities from the database
        /// </summary>
        void LoadCollection<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> childs)
            where TProperty : class;

        /// <summary>
        /// Asynchronously loads the collection of <paramref name="childs"/> entities from the database
        /// </summary>
        Task LoadCollectionAsync<TProperty>(TEntity entity, Expression<Func<TEntity, IEnumerable<TProperty>>> childs,
            CancellationToken cancellationToken)
            where TProperty : class;

        /// <summary>
        /// Asynchronously loads the <paramref name="member"/> entity from the database
        /// </summary>
        Task LoadReferenceAsync<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> member,
            CancellationToken cancellationToken)
            where TProperty : class;

        /// <summary>
        /// Loads the <paramref name="member"/> entity from the database
        /// </summary>
        void LoadReference<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> member)
            where TProperty : class;

        Task<TEntity> GetByIdAsync(CancellationToken cancellationToken, params object[] ids);

        TEntity GetById(params object[] ids);

        Task ReloadAsync(TEntity entity, CancellationToken cancellationToken);

        void Reload(TEntity entity);

        /// <summary>
        /// Returns the query that would be used to load <paramref name="childs"/> collection from the database.
        /// The returned query can be modified using LINQ to perform filtering or operations
        /// in the database, such as counting the number of entities in the collection in
        /// the database without actually loading them
        /// </summary>
        /// <returns>A query for the <paramref name="childs"/> collection</returns>
        IQueryable<TChild> GetCollectionQuery<TChild>(TEntity entity, Expression<Func<TEntity, IEnumerable<TChild>>> childs)
            where TChild : class;
    }
}
