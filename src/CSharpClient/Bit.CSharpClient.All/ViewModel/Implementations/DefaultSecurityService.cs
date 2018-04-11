using Autofac;
using Bit.ViewModel.Contracts;
using IdentityModel.Client;
using Newtonsoft.Json;
using Prism.Autofac;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Auth;

namespace Bit.ViewModel.Implementations
{
    public class DefaultSecurityService : ISecurityService
    {
        private static ISecurityService _current;

        public static ISecurityService Current => _current;

        public DefaultSecurityService(AccountStore accountStore,
            IClientAppProfile clientAppProfile,
            IDateTimeProvider dateTimeProvider,
            IBrowserService browserService,
            IContainerProvider containerProvider)
        {
            _clientAppProfile = clientAppProfile;
            _accountStore = accountStore;
            _dateTimeProvider = dateTimeProvider;
            _browserService = browserService;
            _containerProvider = containerProvider;
            _current = this;
        }

        private readonly IClientAppProfile _clientAppProfile;
        private readonly AccountStore _accountStore;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IBrowserService _browserService;
        private readonly IContainerProvider _containerProvider;

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

        public virtual async Task<Token> Login(object state = null, string client_id = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            await Logout(state, client_id, cancellationToken).ConfigureAwait(false);
            CurrentAction = "Login";
            CurrentLoginTaskCompletionSource = new TaskCompletionSource<Token>();
            _browserService.OpenUrl(GetLoginUrl(state, client_id));
            return await CurrentLoginTaskCompletionSource.Task;
        }

        public virtual async Task<Token> LoginWithCredentials(string username, string password, string client_id, string client_secret, string[] scopes = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            await Logout(state: null, client_id: client_id, cancellationToken: cancellationToken);

            if (scopes == null)
                scopes = "openid profile user_info".Split(' ');

            TokenClient tokenClient = _containerProvider.GetContainer().Resolve<TokenClient>(new NamedParameter("clientId", client_id), new NamedParameter("secret", client_secret));

            TokenResponse tokenResponse = await tokenClient.RequestResourceOwnerPasswordAsync(username, password, scope: string.Join(" ", scopes), cancellationToken: cancellationToken).ConfigureAwait(false);

            if (tokenResponse.IsError)
                throw tokenResponse.Exception ?? new Exception($"{tokenResponse.Error} {tokenResponse.Raw}");

            Account account = Token.FromTokenToAccount(tokenResponse);

            await _accountStore.SaveAsync(account, _clientAppProfile.AppName).ConfigureAwait(false);

            return account;
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
                    CurrentAction = "Logout";
                    CurrentLogoutTaskCompletionSource = new TaskCompletionSource<object>();
                    _browserService.OpenUrl(GetLogoutUrl(token.id_token, state, client_id));
                    await CurrentLogoutTaskCompletionSource.Task;
                }
            }
        }

        public virtual Token GetCurrentToken()
        {
            Account account = GetAccount();

            return account;
        }

        public virtual async Task<Token> GetCurrentTokenAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Account account = await GetAccountAsync().ConfigureAwait(false);

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
                throw new ArgumentException("Id token may not be empty or null", nameof(id_token));

            state = state ?? new { };

            string relativeUri = $"InvokeLogout?state={JsonConvert.SerializeObject(state)}&redirect_uri={_clientAppProfile.OAuthRedirectUri}&id_token={id_token}";

            if (!string.IsNullOrEmpty(client_id))
                relativeUri += $"&client_id={client_id}";

            return new Uri(_clientAppProfile.HostUri, relativeUri: relativeUri);
        }

        protected TaskCompletionSource<Token> CurrentLoginTaskCompletionSource { get; set; }
        protected string CurrentAction { get; set; }
        protected TaskCompletionSource<object> CurrentLogoutTaskCompletionSource { get; set; }

        public virtual async void OnSsoLoginLogoutRedirectCompleted(Uri url)
        {
            Dictionary<string, string> query = (Dictionary<string, string>)WebEx.FormDecode(url.Fragment);

            if (CurrentAction == "Logout")
                CurrentLogoutTaskCompletionSource.SetResult(null);
            else
            {
                Token token = query;

                Account account = Token.FromTokenToAccount(token);

                await _accountStore.SaveAsync(account, _clientAppProfile.AppName).ConfigureAwait(false);

                CurrentLoginTaskCompletionSource.SetResult(query);
            }
        }
    }
}
