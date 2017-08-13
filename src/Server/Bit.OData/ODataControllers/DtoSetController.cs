using Bit.Data.Contracts;
using Bit.Data.Implementations;
using Bit.Model.Contracts;
using Bit.Owin.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData;

namespace Bit.OData.ODataControllers
{
    public class DtoSetController<TDto, TModel, TKey> : DtoController<TDto>
        where TDto : class, IDtoWithDefaultKey<TKey>
        where TModel : class, IEntityWithDefaultKey<TKey>
    {
#if DEBUG
        protected DtoSetController()
        {
        }
#endif

        public DtoSetController(IRepository<TModel> repository)
        {
            if (repository == null)
                throw new ArgumentNullException(nameof(repository));

            Repository = repository;
        }

        protected IRepository<TModel> Repository { get; set; }

        /// <summary>
        /// Optional Dependency. You can override FromDtoToModel and GetAll.
        /// </summary>
        public virtual IDtoModelMapper<TDto, TModel> DtoModelMapper { get; set; }

        public virtual IEnumerable<IDataProviderSpecificMethodsProvider> DataProviderSpecificMethodsProviders { get; set; }

        protected virtual TModel FromDtoToModel(TDto dto)
        {
            return DtoModelMapper.FromDtoToModel(dto);
        }

        [Create]
        public virtual async Task<TDto> Create(TDto dto, CancellationToken cancellationToken)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            TModel model = await Repository.AddAsync(FromDtoToModel(dto), cancellationToken);

            return await GetById(model.Id, cancellationToken);
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

            baseQuery = dataProviderSpecificMethodsProvider.ApplyWhereByKeys(baseQuery, key);

            return baseQuery;
        }

        [Get]
        public virtual async Task<SingleResult<TDto>> Get(TKey key, CancellationToken cancellationToken)
        {
            return SingleResult.Create(await GetByIdQuery(key, cancellationToken));
        }

        [Delete]
        public virtual async Task Delete(TKey key, CancellationToken cancellationToken)
        {
            TModel model = await Repository.GetByIdAsync(cancellationToken, key);
            await Repository.DeleteAsync(model, cancellationToken);
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

            TModel model = FromDtoToModel(dto);

            if (!EqualityComparer<TKey>.Default.Equals(key, model.Id))
                throw new BadRequestException();

            model = await Repository.UpdateAsync(model, cancellationToken);

            return await GetById(key, cancellationToken);
        }

        [Get]
        public virtual async Task<IQueryable<TDto>> GetAll(CancellationToken cancellationToken)
        {
            return DtoModelMapper.FromModelQueryToDtoQuery(await Repository.GetAllAsync(cancellationToken));
        }
    }
}
