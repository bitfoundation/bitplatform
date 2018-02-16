using Bit.ViewModel.Contracts;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.ViewModel.Implementations
{
    public class TokenHandler : DelegatingHandler
    {
        private readonly ISecurityService _securityService;

        public TokenHandler(ISecurityService securityService, HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            _securityService = securityService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Token token = await _securityService.GetCurrentTokenAsync(cancellationToken);

            if (token != null)
                request.Headers.Authorization = new AuthenticationHeaderValue(token.token_type, token.access_token);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
