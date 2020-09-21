using Bit.Core.Contracts;
using Bit.WebApi.Contracts;
using Microsoft.Owin;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Bit.WebApi.Implementations
{
    public class XCorrelationWebApiConfigurationCustomizer : IWebApiConfigurationCustomizer
    {
        public virtual void CustomizeWebApiConfiguration(HttpConfiguration webApiConfiguration)
        {
            if (webApiConfiguration == null)
                throw new ArgumentNullException(nameof(webApiConfiguration));

            webApiConfiguration.MessageHandlers.Add(new XCorrelationHandler { });
        }
    }

    public class XCorrelationHandler : DelegatingHandler
    {
        public const string XCorrelationId = "X-Correlation-ID";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            response.Headers.Add(XCorrelationId, request.GetOwinContext().GetDependencyResolver().Resolve<IRequestInformationProvider>().XCorrelationId);

            return response;
        }
    }
}
