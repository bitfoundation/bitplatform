using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using Bit.Core.Contracts;
using Bit.Model.Dtos;

namespace Bit.OData.ODataControllers
{
    [AllowAnonymous]
    public class ClientsLogsController : DtoController<ClientLogDto>
    {
        public virtual ILogger Logger { get; set; }

        [Action]
        public virtual Task StoreClientLogs(IEnumerable<ClientLogDto> clientLogs)
        {
            Logger.AddLogData("ClientLogs", clientLogs);

            return Logger.LogWarningAsync("Client-Log");
        }

        [Create]
        public virtual async Task<SingleResult<ClientLogDto>> Create(ClientLogDto clientLog)
        {
            Logger.AddLogData("ClientLogs", clientLog);

            await Logger.LogWarningAsync("Client-Log");

            return SingleResult(clientLog);
        }
    }
}
