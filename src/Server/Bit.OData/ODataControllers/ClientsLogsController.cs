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

        public class StoreClientLogsParameters
        {
            public IEnumerable<ClientLogDto> clientLogs { get; set; }
        }

        [Action]
        public virtual Task StoreClientLogs(StoreClientLogsParameters actionParameters)
        {
            Logger.AddLogData("ClientLogs", actionParameters.clientLogs);

            return Logger.LogWarningAsync("Client-Log");
        }

        [Create]
        public virtual async Task<ClientLogDto> Create(ClientLogDto clientLog)
        {
            Logger.AddLogData("ClientLogs", clientLog);

            await Logger.LogWarningAsync("Client-Log");

            return clientLog;
        }
    }
}
