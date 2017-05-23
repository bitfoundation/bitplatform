using Foundation.DataAccess.Contracts;
using Foundation.Model.Contracts;
using System;

namespace Foundation.Api.ApiControllers
{
    public class DefaultDtoSetController<TModel, TDto> : DtoSetController<TModel, TDto, Guid>
        where TDto : class, IDtoWithDefaultGuidKey
        where TModel : class, IEntityWithDefaultGuidKey
    {
        protected DefaultDtoSetController()
            : base()
        {

        }

        public DefaultDtoSetController(IEntityWithDefaultGuidKeyRepository<TModel> repository)
            : base(repository)
        {

        }
    }
}
