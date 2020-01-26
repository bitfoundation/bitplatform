using Bit.Core.Contracts;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Core.Implementations
{
    public class XCorrelationIdHttpMessageHandler : DelegatingHandler
    {
        private readonly IRequestInformationProvider _requestInformationProvider;

        public XCorrelationIdHttpMessageHandler(IRequestInformationProvider requestInformationProvider)
        {
            _requestInformationProvider = requestInformationProvider;
        }

        public XCorrelationIdHttpMessageHandler(IRequestInformationProvider requestInformationProvider, HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            _requestInformationProvider = requestInformationProvider;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!request.Headers.Contains("X-Correlation-ID") && _requestInformationProvider.ContextIsPresent)
                request.Headers.Add("X-Correlation-ID", _requestInformationProvider.XCorrelationId);

            return base.SendAsync(request, cancellationToken);
        }
    }
}
