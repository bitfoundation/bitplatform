using System;
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
        private readonly ILogger _logger;

        public ClientsLogsController(ILogger logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            _logger = logger;
        }

        protected ClientsLogsController()
        {

        }

        public class StoreClientLogsParameters
        {
            public IEnumerable<ClientLogDto> clientLogs { get; set; }
        }

        [Action]
        public virtual async Task StoreClientLogs(StoreClientLogsParameters actionParameters)
        {
            _logger.AddLogData("ClientLogs", actionParameters.clientLogs);

            await _logger.LogWarningAsync("Client-Log");
        }

        [Create]
        public virtual async Task<ClientLogDto> Create(ClientLogDto model)
        {
            _logger.AddLogData("ClientLogs", model);

            await _logger.LogWarningAsync("Client-Log");

            return model;
        }
    }
}
