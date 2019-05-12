using Bit.Data.Contracts;
using Bit.Data.Implementations;
using Bit.Model.Contracts;
using Bit.Model.Implementations;
using Bit.Owin.Exceptions;
using Microsoft.AspNet.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Bit.OData.ODataControllers
{
    public class DtoSetController<TDto, TEntity, TKey> : DtoController<TDto>
        where TDto : class, IDto
        where TEntity : class, IEntity
    {
        public virtual IRepository<TEntity> Repository { get; set; }

        /// <summary>
        /// Optional Dependency. You can override FromDtoToModel and GetAll.
        /// </summary>
        public virtual IDtoEntityMapper<TDto, TEntity> DtoEntityMapper { get; set; }

        public virtual IEnumerable<IDataProviderSpecificMethodsProvider> DataProviderSpecificMethodsProviders { get; set; }

        protected virtual TEntity FromDtoToEntity(TDto dto)
        {
            return DtoEntityMapper.FromDtoToEntity(dto);
        }

        protected virtual TKey GetKey(TDto dto)
        {
            return (TKey)DtoMetadataWorkspace.Current.GetKeys(dto).ExtendedSingle($"Finding keys of ${typeof(TDto).GetTypeInfo().Name}");
        }

        protected async Task<IQueryable<TDto>> GetQueryById(TKey key, CancellationToken cancellationToken)
        {
            IQueryable<TDto> baseQuery = await GetAll(cancellationToken);

            IDataProviderSpecificMethodsProvider dataProviderSpecificMethodsProvider = null;

            if (DataProviderSpecificMethodsProviders != null)
                dataProviderSpecificMethodsProvider = DataProviderSpecificMethodsProviders.FirstOrDefault(asyncQueryableExecutor => asyncQueryableExecutor.SupportsQueryable<TDto>(baseQuery));

            if (dataProviderSpecificMethodsProvider == null)
                dataProviderSpecificMethodsProvider = DefaultDataProviderSpecificMethodsProvider.Current;

            baseQuery = dataProviderSpecificMethodsProvider.ApplyWhereByKeys(baseQuery, key);

            return baseQuery;
        }

        protected virtual async Task<TDto> GetDtoById(TKey key, CancellationToken cancellationToken)
        {
            IQueryable<TDto> getByIdQuery = await GetQueryById(key, cancellationToken);

            IDataProviderSpecificMethodsProvider dataProviderSpecificMethodsProvider = null;

            if (DataProviderSpecificMethodsProviders != null)
                dataProviderSpecificMethodsProvider = DataProviderSpecificMethodsProviders.FirstOrDefault(asyncQueryableExecutor => asyncQueryableExecutor.SupportsQueryable<TDto>(getByIdQuery));

            if (dataProviderSpecificMethodsProvider == null)
                dataProviderSpecificMethodsProvider = DefaultDataProviderSpecificMethodsProvider.Current;

            TDto dto = await dataProviderSpecificMethodsProvider.FirstOrDefaultAsync(getByIdQuery, cancellationToken);

            return dto;
        }

        [Create]
        public virtual async Task<SingleResult<TDto>> Create(TDto dto, CancellationToken cancellationToken)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            TEntity entity = await Repository.AddAsync(FromDtoToEntity(dto), cancellationToken);

            return SingleResult(await GetQueryById(GetKey(DtoEntityMapper.FromEntityToDto(entity)), cancellationToken));
        }

        [Get]
        public virtual async Task<SingleResult<TDto>> Get(TKey key, CancellationToken cancellationToken)
        {
            return SingleResult(await GetQueryById(key, cancellationToken));
        }

        [Delete]
        public virtual async Task Delete(TKey key, CancellationToken cancellationToken)
        {
            TEntity entity = await Repository.GetByIdAsync(cancellationToken, key);
            if (entity == null)
                throw new ResourceNotFoundException();
            await Repository.DeleteAsync(entity, cancellationToken);
        }

        [PartialUpdate]
        public virtual async Task<SingleResult<TDto>> PartialUpdate(TKey key, Delta<TDto> modifiedDtoDelta, CancellationToken cancellationToken)
        {
            if (modifiedDtoDelta == null)
                throw new ArgumentNullException(nameof(modifiedDtoDelta));

            TDto originalDto = await GetDtoById(key, cancellationToken);

            if (originalDto == null)
                throw new ResourceNotFoundException();

            modifiedDtoDelta.Patch(originalDto);

            return await Update(key, originalDto, cancellationToken);
        }

        [Update]
        public virtual async Task<SingleResult<TDto>> Update(TKey key, TDto dto, CancellationToken cancellationToken)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            TEntity entity = FromDtoToEntity(dto);

            if (!EqualityComparer<TKey>.Default.Equals(key, GetKey(dto)))
                throw new BadRequestException();

            entity = await Repository.UpdateAsync(entity, cancellationToken);

            return SingleResult(await GetQueryById(key, cancellationToken));
        }

        [Get]
        public virtual async Task<IQueryable<TDto>> GetAll(CancellationToken cancellationToken)
        {
            return DtoEntityMapper.FromEntityQueryToDtoQuery(await Repository.GetAllAsync(cancellationToken));
        }
    }
}
