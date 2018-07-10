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
        Task<TDto> AddAsync(TDto dtoToAdd, CancellationToken cancellationToken = default(CancellationToken));

        TDto Add(TDto dtoToAdd);

        Task<IEnumerable<TDto>> AddRangeAsync(IEnumerable<TDto> dtosToAdd, CancellationToken cancellationToken = default(CancellationToken));

        IEnumerable<TDto> AddRange(IEnumerable<TDto> dtosToAdd);

        Task<TDto> UpdateAsync(TDto dtoToUpdate, CancellationToken cancellationToken = default(CancellationToken));

        TDto Update(TDto dtoToUpdate);

        Task<TDto> DeleteAsync(TDto dtoToDelete, CancellationToken cancellationToken = default(CancellationToken));

        TDto Delete(TDto dtoToDelete);

        IQueryable<TDto> GetAll();

        Task<IQueryable<TDto>> GetAllAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Loads the collection of <paramref name="childs"/> dtos from the database
        /// </summary>
        void LoadCollection<TProperty>(TDto dto, Expression<Func<TDto, IEnumerable<TProperty>>> childs)
            where TProperty : class;

        /// <summary>
        /// Asynchronously loads the collection of <paramref name="childs"/> dtos from the database
        /// </summary>
        Task LoadCollectionAsync<TProperty>(TDto dto, Expression<Func<TDto, IEnumerable<TProperty>>> childs,
            CancellationToken cancellationToken = default(CancellationToken))
            where TProperty : class;

        /// <summary>
        /// Asynchronously loads the <paramref name="member"/> dto from the database
        /// </summary>
        Task LoadReferenceAsync<TProperty>(TDto dto, Expression<Func<TDto, TProperty>> member,
            CancellationToken cancellationToken = default(CancellationToken))
            where TProperty : class;

        /// <summary>
        /// Loads the <paramref name="member"/> dto from the database
        /// </summary>
        void LoadReference<TProperty>(TDto dto, Expression<Func<TDto, TProperty>> member)
            where TProperty : class;
    }
}
