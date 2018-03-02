using Bit.ViewModel.Contracts;
using IdentityModel.Client;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Auth;
using Xamarin.Auth.Presenters;

namespace Bit.ViewModel.Implementations
{
    public class DefaultSecurityService : ISecurityService
    {
        public DefaultSecurityService(AccountStore accountStore,
            IClientAppProfile clientAppProfile,
            BitOAuth2Authenticator bitOAuth2Authenticator,
            OAuthLoginPresenter oAuthLoginPresenter,
            IDateTimeProvider dateTimeProvider)
        {
            _clientAppProfile = clientAppProfile;
            _accountStore = accountStore;
            _dateTimeProvider = dateTimeProvider;
            OAuthAuthenticator = bitOAuth2Authenticator;
        }

        private readonly IClientAppProfile _clientAppProfile;
        private readonly AccountStore _accountStore;
        private readonly IDateTimeProvider _dateTimeProvider;

        public static BitOAuth2Authenticator OAuthAuthenticator { get; protected set; }

        public virtual bool IsLoggedIn()
        {
            Account account = GetAccount();

            if (account == null)
                return false;

            Token token = account;

            return (_dateTimeProvider.GetCurrentUtcDateTime() - token.login_date) < TimeSpan.FromSeconds(token.expires_in);
        }

        public virtual async Task<bool> IsLoggedInAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Account account = await GetAccountAsync().ConfigureAwait(false);

            if (account == null)
                return false;

            Token token = account;

            return (_dateTimeProvider.GetCurrentUtcDateTime() - token.login_date) < TimeSpan.FromSeconds(token.expires_in);
        }

        private Account GetAccount()
        {
            return (_accountStore.FindAccountsForService(_clientAppProfile.AppName)).SingleOrDefault();
        }

        private async Task<Account> GetAccountAsync()
        {
            return (await _accountStore.FindAccountsForServiceAsync(_clientAppProfile.AppName).ConfigureAwait(false)).SingleOrDefault();
        }

        public virtual Task<Token> Login(object state = null, string client_id = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return OAuthAuthenticator.Login(GetLoginUrl(state, client_id));
        }

        public virtual async Task<Token> LoginWithCredentials(string username, string password, string client_id, string client_secret, string[] scopes = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (scopes == null)
                scopes = "openid profile user_info".Split(' ');

            using (TokenClient tokenClient = new TokenClient(address: new Uri(_clientAppProfile.HostUri, "core/connect/token").ToString(), clientId: client_id, clientSecret: client_secret))
            {
                TokenResponse tokenResponse = await tokenClient.RequestResourceOwnerPasswordAsync(username, password, scope: string.Join(" ", scopes), cancellationToken: cancellationToken).ConfigureAwait(false);

                if (tokenResponse.IsError)
                    throw tokenResponse.Exception ?? new Exception($"{tokenResponse.Error}");

                Account account = Token.FromTokenToAccount(tokenResponse);

                await _accountStore.SaveAsync(account, _clientAppProfile.AppName).ConfigureAwait(false);

                return account;
            }
        }

        public virtual async Task Logout(object state = null, string client_id = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Account account = GetAccount();

            if (account != null)
            {
                Token token = account;
                await _accountStore.DeleteAsync(account, _clientAppProfile.AppName).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(token.id_token))
                {
                    await OAuthAuthenticator.Logout(GetLogoutUrl(token.id_token, state, client_id));
                }
            }
        }

        public virtual Token GetCurrentToken()
        {
            Account account = GetAccount();

            if (account == null)
                return null;

            return account;
        }

        public virtual async Task<Token> GetCurrentTokenAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Account account = await GetAccountAsync().ConfigureAwait(false);

            if (account == null)
                return null;

            return account;
        }

        public virtual Uri GetLoginUrl(object state = null, string client_id = null)
        {
            state = state ?? new { };

            string relativeUri = $"InvokeLogin?state={JsonConvert.SerializeObject(state)}&redirect_uri={ _clientAppProfile.OAuthRedirectUri}";

            if (!string.IsNullOrEmpty(client_id))
                relativeUri += $"&client_id={client_id}";

            return new Uri(_clientAppProfile.HostUri, relativeUri: relativeUri);
        }

        public virtual Uri GetLogoutUrl(string id_token, object state = null, string client_id = null)
        {
            if (string.IsNullOrEmpty(id_token))
                throw new ArgumentException(nameof(id_token));

            state = state ?? new { };

            string relativeUri = $"InvokeLogout?state={JsonConvert.SerializeObject(state)}&redirect_uri={_clientAppProfile.OAuthRedirectUri}&id_token={id_token}";

            if (!string.IsNullOrEmpty(client_id))
                relativeUri += $"&client_id={client_id}";

            return new Uri(_clientAppProfile.HostUri, relativeUri: relativeUri);
        }
    }
}
