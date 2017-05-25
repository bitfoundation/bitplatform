using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Metadata;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData;
using System.Web.Http.Controllers;
using System.Net.Http;
using Bit.Api.ApiControllers;
using Bit.Model.Contracts;
using Bit.Owin.Exceptions;

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

        public override Task<TDto> Update(Guid key, Delta<TDto> modifiedDtoDelta, CancellationToken cancellationToken)
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