using System;
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

        public DefaultDtoSetController(IEntityWithDefaultGuidKeyRepository<TModel> repository)
            : base(repository)
        {

        }
    }
}
