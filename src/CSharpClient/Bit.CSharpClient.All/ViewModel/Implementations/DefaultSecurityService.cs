using Bit.ViewModel.Contracts;
using IdentityModel.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Auth;
using Xamarin.Auth.Presenters;

namespace Bit.ViewModel.Implementations
{
    public class DefaultSecurityService : ISecurityService
    {
        public DefaultSecurityService(AccountStore accountStore, IConfigProvider configProvider, BitOAuth2Authenticator bitOAuth2Authenticator, OAuthLoginPresenter oAuthLoginPresenter, TokenClient tokenClient, IDateTimeProvider dateTimeProvider)
        {
            _configProvider = configProvider;
            _accountStore = accountStore;
            _oAuthLoginPresenter = oAuthLoginPresenter;
            _tokenClient = tokenClient;
            _dateTimeProvider = dateTimeProvider;
            BitOAuth2Authenticator = _bitOAuth2Authenticator = bitOAuth2Authenticator;
        }

        private readonly IConfigProvider _configProvider;
        private readonly BitOAuth2Authenticator _bitOAuth2Authenticator;
        private readonly AccountStore _accountStore;
        private readonly OAuthLoginPresenter _oAuthLoginPresenter;
        private readonly TokenClient _tokenClient;
        private readonly IDateTimeProvider _dateTimeProvider;

        public static BitOAuth2Authenticator BitOAuth2Authenticator { get; protected set; }

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
            Account account = await GetAccountAsync();

            if (account == null)
                return false;

            Token token = account;

            return (_dateTimeProvider.GetCurrentUtcDateTime() - token.login_date) < TimeSpan.FromSeconds(token.expires_in);
        }

        private Account GetAccount()
        {
            return (_accountStore.FindAccountsForService(_configProvider.AppName)).SingleOrDefault();
        }

        private async Task<Account> GetAccountAsync()
        {
            return (await _accountStore.FindAccountsForServiceAsync(_configProvider.AppName)).SingleOrDefault();
        }

        public virtual Task<Token> Login(object state = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            state = state ?? new { };

            BitOAuth2Authenticator.State = state;

            TaskCompletionSource<Token> taskSource = new TaskCompletionSource<Token>();

            async void completed(object sender, AuthenticatorCompletedEventArgs e)
            {
                _bitOAuth2Authenticator.Completed -= completed;
                _bitOAuth2Authenticator.Error -= error;

                if (e.IsAuthenticated)
                {
                    if (e.Account != null)
                    {
                        try
                        {
                            await Logout(internalAppLogoutOnly: true);
                        }
                        catch { }

                        if (!e.Account.Properties.ContainsKey(nameof(Token.login_date)))
                            e.Account.Properties.Add(nameof(Token.login_date), Convert.ToString(_dateTimeProvider.GetCurrentUtcDateTime()));

                        await _accountStore.SaveAsync(e.Account, _configProvider.AppName);
                    }

                    taskSource.SetResult(e.Account);
                }
                else
                {
                    throw new InvalidOperationException("Authentication failed");
                }
            };

            void error(object sender, AuthenticatorErrorEventArgs e)
            {
                _bitOAuth2Authenticator.Completed -= completed;
                _bitOAuth2Authenticator.Error -= error;

                taskSource.SetException(e.Exception ?? new Exception(e.Message));
            };

            _bitOAuth2Authenticator.Completed += completed;
            _bitOAuth2Authenticator.Error += error;

            _oAuthLoginPresenter.Login(_bitOAuth2Authenticator);

            return taskSource.Task;
        }

        public virtual async Task<Token> LoginWithCredentials(string username, string password, CancellationToken cancellationToken = default(CancellationToken))
        {
            TokenResponse tokenResponse = await _tokenClient.RequestResourceOwnerPasswordAsync(username, password, scope: "openid profile user_info");

            if (tokenResponse.IsError)
                throw tokenResponse.Exception ?? new Exception($"{tokenResponse.Error}");

            try
            {
                await Logout(internalAppLogoutOnly: true);
            }
            catch { }

            string[] tokenParts = tokenResponse.AccessToken.Split('.');
            byte[] decodedByteArrayToken = Convert.FromBase64String(tokenParts[1]);
            string decodedStringToken = Encoding.UTF8.GetString(decodedByteArrayToken);
            JObject jwtToken = JObject.Parse(decodedStringToken);
            string userName = jwtToken["sub"].Value<string>();

            Account account = new Account(username, new Dictionary<string, string>
            {
                { "access_token" , tokenResponse.AccessToken },
                { "expires_in" , tokenResponse.ExpiresIn.ToString()},
                { "token_type" , tokenResponse.TokenType },
                { "login_date", Convert.ToString(_dateTimeProvider.GetCurrentUtcDateTime()) }
            });

            await _accountStore.SaveAsync(account, _configProvider.AppName);

            return account;
        }

        public virtual async Task Logout(CancellationToken cancellationToken = default(CancellationToken))
        {
            await Logout(internalAppLogoutOnly: false);
        }

        private async Task Logout(bool internalAppLogoutOnly)
        {
            Account account = GetAccount();

            if (account != null)
            {
                await _accountStore.DeleteAsync(account, _configProvider.AppName);
            }

            if (internalAppLogoutOnly == false)
            {
                WebAuthenticator.ClearCookies(); // This won't work due security reasons >> We need to store id_token + calling InvokeLogout?id_token=...
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
            Account account = await GetAccountAsync();

            if (account == null)
                return null;

            return account;
        }
    }
}
