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
    /// <typeparam name="TDto">Dto class with <see cref="Bit.Model.Contracts.IDto"/> marker</typeparam>
    public interface IRepository<TDto>
        where TDto : class, IDto
    {
        Task<TDto> AddAsync(TDto dtoToAdd, CancellationToken cancellationToken = default);

        TDto Add(TDto dtoToAdd);

        Task<IEnumerable<TDto>> AddRangeAsync(IEnumerable<TDto> dtosToAdd, CancellationToken cancellationToken = default);

        IEnumerable<TDto> AddRange(IEnumerable<TDto> dtosToAdd);

        Task<TDto> UpdateAsync(TDto dtoToUpdate, CancellationToken cancellationToken = default);

        TDto Update(TDto dtoToUpdate);

        Task<TDto> DeleteAsync(TDto dtoToDelete, CancellationToken cancellationToken = default);

        TDto Delete(TDto dtoToDelete);

        IQueryable<TDto> GetAll();

        Task<IQueryable<TDto>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads the collection of <paramref name="childs"/> dtos from the database
        /// </summary>
        void LoadCollection<TProperty>(TDto dto, Expression<Func<TDto, IEnumerable<TProperty>>> childs)
            where TProperty : class;

        /// <summary>
        /// Asynchronously loads the collection of <paramref name="childs"/> dtos from the database
        /// </summary>
        Task LoadCollectionAsync<TProperty>(TDto dto, Expression<Func<TDto, IEnumerable<TProperty>>> childs,
            CancellationToken cancellationToken = default)
            where TProperty : class;

        /// <summary>
        /// Asynchronously loads the <paramref name="member"/> dto from the database
        /// </summary>
        Task LoadReferenceAsync<TProperty>(TDto dto, Expression<Func<TDto, TProperty>> member,
            CancellationToken cancellationToken = default)
            where TProperty : class;

        /// <summary>
        /// Loads the <paramref name="member"/> dto from the database
        /// </summary>
        void LoadReference<TProperty>(TDto dto, Expression<Func<TDto, TProperty>> member)
            where TProperty : class;

        Task ReloadAsync(TDto dto, CancellationToken cancellationToken);

        void Reload(TDto dto);

        /// <summary>
        /// Returns the query that would be used to load <paramref name="childs"/> collection from the database.
        /// The returned query can be modified using LINQ to perform filtering or operations
        /// in the database, such as counting the number of entities in the collection in
        /// the database without actually loading them
        /// </summary>
        /// <returns>A query for the <paramref name="childs"/> collection</returns>
        IQueryable<TChild> GetCollectionQuery<TChild>(TDto dto, Expression<Func<TDto, IEnumerable<TChild>>> childs)
            where TChild : class;
    }
}
