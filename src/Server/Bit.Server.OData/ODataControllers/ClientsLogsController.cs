using Bit.Core.Contracts;
using Bit.Model.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;

namespace Bit.OData.ODataControllers
{
    [AllowAnonymous]
    public class ClientsLogsController : DtoController<ClientLogDto>
    {
        public virtual ILogger Logger { get; set; } = default!;

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

            await Logger.LogWarningAsync("Client-Log").ConfigureAwait(false);

            return SingleResult(clientLog);
        }
    }
}
