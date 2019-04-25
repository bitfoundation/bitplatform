using Bit.Model.Contracts;
using Bit.OData.ODataControllers;

namespace Bit.Tests.Api.ApiControllers_JustForTestDuplicateSchemaIdInSwagger
{
    public class ClientLogDto : IDto
    {
        public virtual int Id { get; set; }
    }

    public class ClientLogsController : DtoController<ClientLogDto>
    {
        [Function]
        public virtual ClientLogDto Test()
        {
            return new ClientLogDto { Id = 1 };
        }
    }
}
