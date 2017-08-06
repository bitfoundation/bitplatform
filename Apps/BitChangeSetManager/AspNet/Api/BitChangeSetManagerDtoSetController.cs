using Bit.Model.Contracts;
using Bit.OData.ODataControllers;
using Bit.Owin.Exceptions;
using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Metadata;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData;

namespace BitChangeSetManager.Api
{
    public class BitChangeSetManagerDtoSetController<TDto, TModel> : DefaultDtoSetController<TDto, TModel>
        where TDto : class, IDtoWithDefaultGuidKey
        where TModel : class, IEntityWithDefaultGuidKey
    {
        private readonly IBitChangeSetManagerRepository<TModel> _repository;

        public BitChangeSetManagerDtoSetController(IBitChangeSetManagerRepository<TModel> repository)
            : base(repository)
        {
            _repository = repository;
        }

        [Create]
        public override async Task<TDto> Create(TDto dto, CancellationToken cancellationToken)
        {
            if (IsReadOnly())
                throw new AppException(BitChangeSetManagerMetadata.InsertIsDeined);
            else
                return await base.Create(dto, cancellationToken);
        }

        [PartialUpdate]
        public override async Task<TDto> PartialUpdate(Guid key, Delta<TDto> modifiedDtoDelta, CancellationToken cancellationToken)
        {
            if (IsReadOnly())
                throw new AppException(BitChangeSetManagerMetadata.UpdateIsDeined);
            else
                return await base.PartialUpdate(key, modifiedDtoDelta, cancellationToken);
        }

        [Delete]
        public override async Task Delete(Guid key, CancellationToken cancellationToken)
        {
            if (IsReadOnly())
                throw new AppException(BitChangeSetManagerMetadata.DeleteIsDeined);
            else
                await base.Delete(key, cancellationToken);
        }

        [Update]
        public override Task<TDto> Update(Guid key, TDto dto, CancellationToken cancellationToken)
        {
            if (IsReadOnly())
                throw new AppException(BitChangeSetManagerMetadata.DeleteIsDeined);
            else
                return base.Update(key, dto, cancellationToken);
        }

        protected virtual bool IsReadOnly()
        {
            return false;
        }
    }
}