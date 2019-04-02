using Bit.Model.Events;
using Bit.ViewModel.Contracts;
using Prism.Events;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.ViewModel.Implementations
{
    public class AuthenticatedHttpMessageHandler : DelegatingHandler
    {
        private readonly ISecurityService _securityService;
        private readonly IEventAggregator _eventAggregator;

        public AuthenticatedHttpMessageHandler(IEventAggregator eventAggregator, ISecurityService securityService, HttpMessageHandler defaultHttpMessageHandler)
            : base(defaultHttpMessageHandler)
        {
            _securityService = securityService;
            _eventAggregator = eventAggregator;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Authorization == null)
            {
                Token token = await _securityService.GetCurrentTokenAsync(cancellationToken).ConfigureAwait(false);

                if (token != null)
                    request.Headers.Authorization = new AuthenticationHeaderValue(token.token_type, token.access_token);
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && (await _securityService.IsLoggedInAsync(cancellationToken).ConfigureAwait(false)) == false)
            {
                _eventAggregator.GetEvent<TokenExpiredEvent>().Publish(new TokenExpiredEvent { });
            }

            return response;
        }
    }
}
