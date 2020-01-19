using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Bit.WebApi.Contracts;

namespace Bit.WebApi.Implementations
{
    public class ClientCorrelationWebApiConfigurationCustomizer : IWebApiConfigurationCustomizer
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.MessageHandlers.Add(new ClientCorrelationHandler { });
        }
    }

    public class ClientCorrelationHandler : DelegatingHandler
    {
        public const string XCorrelationId = "X-Correlation-ID";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            response.Headers.Add(XCorrelationId, request.Headers.GetValues(XCorrelationId));

            return response;
        }
    }
}
