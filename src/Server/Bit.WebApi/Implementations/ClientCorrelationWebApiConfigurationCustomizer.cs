using Bit.WebApi.Contracts;
using Correlator.Handlers;
using System.Web.Http;

namespace Bit.WebApi.Implementations
{
    public class ClientCorrelationWebApiConfigurationCustomizer : IWebApiConfigurationCustomizer
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.MessageHandlers.Add(new ClientCorrelationHandler { Propagate = true, InitializeIfEmpty = true });
        }
    }
}
