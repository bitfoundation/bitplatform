using Bit.Model.Contracts;
using Bit.OData.ODataControllers;
using Bit.Owin.Exceptions;
using BitChangeSetManager.Metadata;
using Microsoft.AspNet.OData;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace BitChangeSetManager.Api
{
    public class BitChangeSetManagerDtoSetController<TDto, TModel, TKey> : DtoSetController<TDto, TModel, TKey>
        where TDto : class, IDto
        where TModel : class, IEntity
    {
        [Create]
        public override async Task<SingleResult<TDto>> Create(TDto dto, CancellationToken cancellationToken)
        {
            if (IsReadOnly())
                throw new AppException(BitChangeSetManagerMetadata.InsertIsDeined);
            else
                return await base.Create(dto, cancellationToken);
        }

        [PartialUpdate]
        public override async Task<SingleResult<TDto>> PartialUpdate(TKey key, Delta<TDto> modifiedDtoDelta, CancellationToken cancellationToken)
        {
            if (IsReadOnly())
                throw new AppException(BitChangeSetManagerMetadata.UpdateIsDeined);
            else
                return await base.PartialUpdate(key, modifiedDtoDelta, cancellationToken);
        }

        [Delete]
        public override async Task Delete(TKey key, CancellationToken cancellationToken)
        {
            if (IsReadOnly())
                throw new AppException(BitChangeSetManagerMetadata.DeleteIsDeined);
            else
                await base.Delete(key, cancellationToken);
        }

        [Update]
        public override Task<SingleResult<TDto>> Update(TKey key, TDto dto, CancellationToken cancellationToken)
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