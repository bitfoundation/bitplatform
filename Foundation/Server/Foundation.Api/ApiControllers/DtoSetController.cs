using Foundation.Api.Exceptions;
using Foundation.DataAccess.Contracts;
using Foundation.Model.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData;

namespace Foundation.Api.ApiControllers
{
    public class DtoSetController<TModel, TDto, TKey> : DtoController<TDto>
        where TDto : class, IDtoWithDefaultKey<TKey>
        where TModel : class, IEntityWithDefaultKey<TKey>
    {
        protected DtoSetController()
        {

        }

        public DtoSetController(IEntityWithDefaultKeyRepository<TModel, TKey> repository)
        {
            if (repository == null)
                throw new ArgumentNullException(nameof(repository));

            _repository = repository;
        }

        private readonly IEntityWithDefaultKeyRepository<TModel, TKey> _repository;


        /// <summary>
        /// Optional Dependency. You can override FromDtoToModel and GetAll.
        /// </summary>
        public virtual IDtoModelMapper<TDto, TModel> DtoModelMapper { get; set; }

        public virtual IEnumerable<IWhereByKeyBuilder<TDto, TKey>> WhereByKeyBuilders { get; set; }

        public virtual IEnumerable<IAsyncQueryableExecuter> AsyncQueryableExecuters { get; set; }

        protected virtual TModel FromDtoToModel(TDto dto)
        {
            return DtoModelMapper.FromDtoToModel(dto);
        }

        [Create]
        public virtual async Task<TDto> Insert(TDto dto, CancellationToken cancellationToken)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            TModel model = await _repository.AddAsync(FromDtoToModel(dto), cancellationToken);

            return await Get(model.Id, cancellationToken);
        }

        [Get]
        public virtual async Task<TDto> Get([FromODataUri]TKey key, CancellationToken cancellationToken)
        {
            IQueryable<TDto> baseQuery = WhereByKeyBuilders.WhereByKey(GetAll(), key);

            IAsyncQueryableExecuter asyncQueryableExecuterToUser = null;

            if (AsyncQueryableExecuters != null)
                asyncQueryableExecuterToUser = AsyncQueryableExecuters.FirstOrDefault(asyncQueryableExecuter => asyncQueryableExecuter.SupportsAsyncExecution<TDto>(baseQuery));

            TDto dtoResult = default(TDto);

            if (asyncQueryableExecuterToUser != null)
                dtoResult = await asyncQueryableExecuterToUser.FirstOrDefaultAsync(baseQuery, cancellationToken);
            else
                dtoResult = baseQuery.FirstOrDefault();

            if (dtoResult == null)
                throw new ResourceNotFoundException();

            return dtoResult;
        }

        [Delete]
        public virtual async Task Delete([FromODataUri]TKey key, CancellationToken cancellationToken)
        {
            TModel model = await _repository.GetByIdAsync(key, cancellationToken);
            await _repository.DeleteAsync(model, cancellationToken);
        }

        [PartialUpdate]
        public virtual async Task<TDto> Update([FromODataUri]TKey key, Delta<TDto> modifiedDtoDelta, CancellationToken cancellationToken)
        {
            if (modifiedDtoDelta == null)
                throw new ArgumentNullException(nameof(modifiedDtoDelta));

            TDto originalDto = await Get(key, cancellationToken);

            if (originalDto == null)
                throw new ResourceNotFoundException();

            modifiedDtoDelta.Patch(originalDto);

            return await Update(key, originalDto, cancellationToken);
        }

        [Update]
        public virtual async Task<TDto> Update([FromODataUri] TKey key, TDto dto, CancellationToken cancellationToken)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            TModel model = FromDtoToModel(dto);

            if (!EqualityComparer<TKey>.Default.Equals(key, model.Id))
                throw new BadRequestException();

            model = await _repository.UpdateAsync(model, cancellationToken);

            return await Get(key, cancellationToken);
        }

        [Get]
        public virtual IQueryable<TDto> GetAll()
        {
            return DtoModelMapper.FromModelQueryToDtoQuery(_repository.GetAll());
        }
    }
}
