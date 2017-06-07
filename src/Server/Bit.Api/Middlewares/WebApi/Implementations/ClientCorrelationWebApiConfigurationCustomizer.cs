using System.Web.Http;
using Bit.Api.Middlewares.WebApi.Contracts;
using Correlator.Handlers;

namespace Bit.Api.Middlewares.WebApi.Implementations
{
    public class ClientCorrelationWebApiConfigurationCustomizer : IWebApiConfigurationCustomizer
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.MessageHandlers.Add(new ClientCorrelationHandler { Propagate = true, InitializeIfEmpty = true });
        }
    }
}
