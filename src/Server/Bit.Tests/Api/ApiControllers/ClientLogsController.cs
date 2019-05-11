using Bit.Model.Contracts;
using Bit.OData.ODataControllers;
using System.Web.Http;

namespace Bit.Tests.Api.ApiControllers_JustForTestDuplicateSchemaIdInSwagger
{
    public class ClientLogDto : IDto
    {
        public virtual int Id { get; set; }
    }

    public class ClientLogsController : DtoController<ClientLogDto>
    {
        [Function]
        public virtual SingleResult<ClientLogDto> Test()
        {
            return SingleResult(new ClientLogDto { Id = 1 });
        }
    }
}
