using Correlator.Handlers;
using Foundation.Api.Middlewares.WebApi.Contracts;
using System.Web.Http;

namespace Foundation.Api.Middlewares.WebApi.Implementations
{
    public class ClientCorrelationWebApiConfigurationCustomizer : IWebApiConfigurationCustomizer
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.MessageHandlers.Add(new ClientCorrelationHandler { Propagate = true, InitializeIfEmpty = true });
        }
    }
}
