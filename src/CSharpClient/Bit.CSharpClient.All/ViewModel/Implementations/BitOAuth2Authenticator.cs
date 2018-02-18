using Bit.ViewModel.Contracts;
using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Auth;

namespace Bit.ViewModel.Implementations
{
    public class BitOAuth2Authenticator : OAuth2Authenticator
    {
        private readonly IConfigProvider _configProvider;

        public BitOAuth2Authenticator(IConfigProvider configProvider)
            : base(clientId: configProvider.OAuthImplicitFlowClientId,
                  scope: "openid profile user_info",
                  authorizeUrl: new Uri(configProvider.HostUri, relativeUri: "core/connect/authorize"),
                  redirectUrl: configProvider.OAuthImplicitFlowRedirectUri,
                  getUsernameAsync: null,
                  isUsingNativeUI: true)
        {
            _configProvider = configProvider;
        }

        public override async Task<Uri> GetInitialUrlAsync()
        {
            Uri originalUri = await base.GetInitialUrlAsync();

            NameValueCollection queryString = originalUri.ParseQueryString();
            queryString.Set("response_type", "id_token token");
            queryString.Set("nonce", Guid.NewGuid().ToString("N"));
            queryString.Set("client_id", ClientId);
            queryString.Set("redirect_uri", _configProvider.OAuthImplicitFlowRedirectUri.ToString());
            queryString.Set("scope", Scope);
            queryString.Set("state", JsonConvert.SerializeObject(State));

            return new Uri($"{AuthorizeUrl}?{queryString}");
        }

        internal object State { get; set; } = new { };
    }
}
