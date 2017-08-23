using System;
using Bit.Data.Contracts;
using Bit.Model.Contracts;

namespace Bit.OData.ODataControllers
{
    public class DefaultDtoSetController<TDto, TEntity> : DtoSetController<TDto, TEntity, Guid>
        where TDto : class, IDtoWithDefaultGuidKey
        where TEntity : class, IEntityWithDefaultGuidKey
    {
#if DEBUG
        protected DefaultDtoSetController()
        {
        }
#endif

        public DefaultDtoSetController(IRepository<TEntity> repository)
            : base(repository)
        {

        }
    }
}
