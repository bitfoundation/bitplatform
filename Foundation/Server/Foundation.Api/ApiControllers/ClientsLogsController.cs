using Foundation.Core.Contracts;
using Foundation.Model.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData;

namespace Foundation.Api.ApiControllers
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

        [Action]
        [Parameter("clientLogs", typeof(IEnumerable<ClientLogDto>))]
        public virtual async Task StoreClientLogs(ODataActionParameters actionParameters)
        {
            _logger.AddLogData("ClientLogs", actionParameters["clientLogs"]);

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
