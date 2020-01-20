using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Bit.Core.Contracts;
using Bit.WebApi.Contracts;
using Microsoft.Owin;

namespace Bit.WebApi.Implementations
{
    public class XCorrelationWebApiConfigurationCustomizer : IWebApiConfigurationCustomizer
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            webApiConfiguration.MessageHandlers.Add(new XCorrelationHandler { });
        }
    }

    public class XCorrelationHandler : DelegatingHandler
    {
        public const string XCorrelationId = "X-Correlation-ID";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            response.Headers.Add(XCorrelationId, request.GetOwinContext().GetDependencyResolver().Resolve<IRequestInformationProvider>().XCorrelationId);

            return response;
        }
    }
}
