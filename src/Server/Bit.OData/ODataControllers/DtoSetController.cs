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

        [Create]
        public virtual async Task<TDto> Create(TDto dto, CancellationToken cancellationToken)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            TEntity entity = await Repository.AddAsync(FromDtoToEntity(dto), cancellationToken);

            try
            {
                return await GetById(GetKey(DtoEntityMapper.FromEntityToDto(entity)), cancellationToken);
            }
            catch (ResourceNotFoundException)
            {
                return DtoEntityMapper.FromEntityToDto(entity);
            }
        }

        protected virtual TKey GetKey(TDto dto)
        {
            return (TKey)DtoMetadataWorkspace.Current.GetKeys(dto).ExtendedSingle($"Finding keys of ${typeof(TDto).GetTypeInfo().Name}");
        }

        protected virtual async Task<TDto> GetById(TKey key, CancellationToken cancellationToken)
        {
            IDataProviderSpecificMethodsProvider dataProviderSpecificMethodsProvider = null;

            IQueryable<TDto> getByIdQuery = await GetByIdQuery(key, cancellationToken);

            if (DataProviderSpecificMethodsProviders != null)
                dataProviderSpecificMethodsProvider = DataProviderSpecificMethodsProviders.FirstOrDefault(asyncQueryableExecuter => asyncQueryableExecuter.SupportsQueryable<TDto>(getByIdQuery));

            if (dataProviderSpecificMethodsProvider == null)
                dataProviderSpecificMethodsProvider = DefaultDataProviderSpecificMethodsProvider.Current;

            TDto result = await dataProviderSpecificMethodsProvider.FirstOrDefaultAsync(getByIdQuery, cancellationToken);

            if (result == null)
                throw new ResourceNotFoundException();

            return result;
        }

        private async Task<IQueryable<TDto>> GetByIdQuery(TKey key, CancellationToken cancellationToken)
        {
            IQueryable<TDto> baseQuery = await GetAll(cancellationToken);

            IDataProviderSpecificMethodsProvider dataProviderSpecificMethodsProvider = null;

            if (DataProviderSpecificMethodsProviders != null)
                dataProviderSpecificMethodsProvider = DataProviderSpecificMethodsProviders.FirstOrDefault(asyncQueryableExecuter => asyncQueryableExecuter.SupportsQueryable<TDto>(baseQuery));

            if (dataProviderSpecificMethodsProvider == null)
                dataProviderSpecificMethodsProvider = DefaultDataProviderSpecificMethodsProvider.Current;

            return dataProviderSpecificMethodsProvider.ApplyWhereByKeys(baseQuery, key);
        }

        [Get]
        public virtual async Task<SingleResult<TDto>> Get(TKey key, CancellationToken cancellationToken)
        {
            return SingleResult.Create(await GetByIdQuery(key, cancellationToken));
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
        public virtual async Task<TDto> PartialUpdate(TKey key, Delta<TDto> modifiedDtoDelta, CancellationToken cancellationToken)
        {
            if (modifiedDtoDelta == null)
                throw new ArgumentNullException(nameof(modifiedDtoDelta));

            TDto originalDto = await GetById(key, cancellationToken);

            if (originalDto == null)
                throw new ResourceNotFoundException();

            modifiedDtoDelta.Patch(originalDto);

            return await Update(key, originalDto, cancellationToken);
        }

        [Update]
        public virtual async Task<TDto> Update(TKey key, TDto dto, CancellationToken cancellationToken)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            TEntity entity = FromDtoToEntity(dto);

            if (!EqualityComparer<TKey>.Default.Equals(key, GetKey(dto)))
                throw new BadRequestException();

            entity = await Repository.UpdateAsync(entity, cancellationToken);

            try
            {
                return await GetById(key, cancellationToken);
            }
            catch (ResourceNotFoundException)
            {
                return DtoEntityMapper.FromEntityToDto(entity);
            }
        }

        [Get]
        public virtual async Task<IQueryable<TDto>> GetAll(CancellationToken cancellationToken)
        {
            return DtoEntityMapper.FromEntityQueryToDtoQuery(await Repository.GetAllAsync(cancellationToken));
        }
    }
}
