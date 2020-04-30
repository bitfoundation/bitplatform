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
    public interface IRepository<T>
        where T : class
    {
        Task<T> AddAsync(T itemToAdd, CancellationToken cancellationToken);

        T Add(T itemToAdd);

        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entitiesToAdd, CancellationToken cancellationToken);

        IEnumerable<T> AddRange(IEnumerable<T> entitiesToAdd);

        Task<T> UpdateAsync(T itemToUpdate, CancellationToken cancellationToken);

        T Update(T itemToUpdate);

        Task<T> DeleteAsync(T itemToDelete, CancellationToken cancellationToken);

        T Delete(T itemToDelete);

        IQueryable<T> GetAll();

        Task<IQueryable<T>> GetAllAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Loads the collection of <paramref name="childs"/> entities from the database
        /// </summary>
        void LoadCollection<TProperty>(T item, Expression<Func<T, IEnumerable<TProperty?>>> childs)
            where TProperty : class;

        /// <summary>
        /// Asynchronously loads the collection of <paramref name="childs"/> entities from the database
        /// </summary>
        Task LoadCollectionAsync<TProperty>(T item, Expression<Func<T, IEnumerable<TProperty?>>> childs,
            CancellationToken cancellationToken)
            where TProperty : class;

        /// <summary>
        /// Asynchronously loads the <paramref name="member"/> item from the database
        /// </summary>
        Task LoadReferenceAsync<TProperty>(T item, Expression<Func<T, TProperty?>> member,
            CancellationToken cancellationToken)
            where TProperty : class;

        /// <summary>
        /// Loads the <paramref name="member"/> item from the database
        /// </summary>
        void LoadReference<TProperty>(T item, Expression<Func<T, TProperty?>> member)
            where TProperty : class;

        Task<T?> GetByIdAsync(CancellationToken cancellationToken, params object[] ids);

        T? GetById(params object[] ids);

        Task ReloadAsync(T item, CancellationToken cancellationToken);

        void Reload(T item);

        /// <summary>
        /// Returns the query that would be used to load <paramref name="childs"/> collection from the database.
        /// The returned query can be modified using LINQ to perform filtering or operations
        /// in the database, such as counting the number of entities in the collection in
        /// the database without actually loading them
        /// </summary>
        /// <returns>A query for the <paramref name="childs"/> collection</returns>
        IQueryable<TChild> GetCollectionQuery<TChild>(T item, Expression<Func<T, IEnumerable<TChild>>> childs)
            where TChild : class;
    }
}
