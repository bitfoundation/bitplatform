using Bit.Model.Contracts;
using Bit.OData.ODataControllers;
using Bit.Owin.Exceptions;
using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Metadata;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
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

        public override Task<TDto> Create(TDto dto, CancellationToken cancellationToken)
        {
            throw new AppException(BitChangeSetManagerMetadata.InsertIsDeined);
        }

        public override Task<TDto> PartialUpdate(Guid key, Delta<TDto> modifiedDtoDelta, CancellationToken cancellationToken)
        {
            throw new AppException(BitChangeSetManagerMetadata.UpdateIsDeined);
        }

        public override Task Delete(Guid key, CancellationToken cancellationToken)
        {
            throw new AppException(BitChangeSetManagerMetadata.DeleteIsDeined);
        }

        protected override void Initialize(HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext);

            if (Request.Method != HttpMethod.Get)
                throw new AppException(BitChangeSetManagerMetadata.OnlyGetIsAllowed);
        }
    }
}