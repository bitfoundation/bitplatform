using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData;
using Bit.Data.Contracts;
using Bit.Model.Contracts;

namespace Bit.OData.ODataControllers
{
    public class DefaultDtoSetController<TDto, TModel> : DtoSetController<TDto, TModel, Guid>
        where TDto : class, IDtoWithDefaultGuidKey
        where TModel : class, IEntityWithDefaultGuidKey
    {
#if DEBUG
        protected DefaultDtoSetController()
        {
        }
#endif

        public DefaultDtoSetController(IRepository<TModel> repository)
            : base(repository)
        {

        }

        [Create]
        public override Task<TDto> Create(TDto dto, CancellationToken cancellationToken)
        {
            return base.Create(dto, cancellationToken);
        }

        [Update]
        public override Task<TDto> Update(Guid key, TDto dto, CancellationToken cancellationToken)
        {
            return base.Update(key, dto, cancellationToken);
        }

        [PartialUpdate]
        public override Task<TDto> PartialUpdate(Guid key, Delta<TDto> modifiedDtoDelta, CancellationToken cancellationToken)
        {
            return base.PartialUpdate(key, modifiedDtoDelta, cancellationToken);
        }

        [Get]
        public override Task<SingleResult<TDto>> Get(Guid key, CancellationToken cancellationToken)
        {
            return base.Get(key, cancellationToken);
        }

        [Get]
        public override Task<IQueryable<TDto>> GetAll(CancellationToken cancellationToken)
        {
            return base.GetAll(cancellationToken);
        }

        [Delete]
        public override Task Delete(Guid key, CancellationToken cancellationToken)
        {
            return base.Delete(key, cancellationToken);
        }
    }
}
