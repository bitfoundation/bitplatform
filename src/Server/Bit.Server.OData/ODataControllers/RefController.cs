using Bit.Model.Contracts;
using System;
using System.Web.Http.Description;

namespace Bit.OData.ODataControllers
{
    public class RefDto : IDto
    {
        public int Id { get; set; }
    }

    public class RefController : DtoController<RefDto>
    {
        [Action, ApiExplorerSettings(IgnoreApi = true)]
        public virtual void HandleRef()
        {

        }
    }
}
