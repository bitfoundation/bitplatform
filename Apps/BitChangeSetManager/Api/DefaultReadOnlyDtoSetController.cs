using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Metadata;
using Foundation.Api.ApiControllers;
using Foundation.Api.Exceptions;
using Foundation.Model.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData;

namespace BitChangeSetManager.Api
{
    public class DefaultReadOnlyDtoSetController<TModel, TDto> : DefaultDtoSetController<TModel, TDto>
        where TDto : class, IDtoWithDefaultGuidKey
        where TModel : class, IEntityWithDefaultGuidKey
    {
        private readonly IBitChangeSetManagerRepository<TModel> _repository;

        public DefaultReadOnlyDtoSetController(IBitChangeSetManagerRepository<TModel> repository)
            : base(repository)
        {
            _repository = repository;
        }

        public override Task<TDto> Insert(TDto dto, CancellationToken cancellationToken)
        {
            throw new AppException(BitChangeSetManagerMetadata.InsertIsDeined);
        }

        public override Task<TDto> Update([FromODataUri] Guid key, Delta<TDto> modifiedDtoDelta, CancellationToken cancellationToken)
        {
            throw new AppException(BitChangeSetManagerMetadata.UpdateIsDeined);
        }

        public override Task Delete([FromODataUri] Guid key, CancellationToken cancellationToken)
        {
            throw new AppException(BitChangeSetManagerMetadata.DeleteIsDeined);
        }
    }
}