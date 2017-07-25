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
    }
}
