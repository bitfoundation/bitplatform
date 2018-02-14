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
        public DefaultSecurityService(AccountStore accountStore, IConfigProvider configProvider, BitOAuth2Authenticator bitOAuth2Authenticator, OAuthLoginPresenter oAuthLoginPresenter, TokenClient tokenClient)
        {
            _configProvider = configProvider;
            _accountStore = accountStore;
            _oAuthLoginPresenter = oAuthLoginPresenter;
            _tokenClient = tokenClient;
            BitOAuth2Authenticator = _bitOAuth2Authenticator = bitOAuth2Authenticator;
        }

        private readonly IConfigProvider _configProvider;
        private readonly BitOAuth2Authenticator _bitOAuth2Authenticator;
        private readonly AccountStore _accountStore;
        private readonly OAuthLoginPresenter _oAuthLoginPresenter;
        private readonly TokenClient _tokenClient;

        public static BitOAuth2Authenticator BitOAuth2Authenticator { get; protected set; }

        public virtual async Task<bool> IsLoggedIn(CancellationToken cancellationToken = default(CancellationToken))
        {
            return (await GetAccount(cancellationToken)) != null;
        }

        private async Task<Account> GetAccount(CancellationToken cancellationToken)
        {
            return (await _accountStore.FindAccountsForServiceAsync(_configProvider.AppName)).SingleOrDefault();
        }

        public virtual Task<(string access_token, long expires_in, string token_type)> Login(object state = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            state = state ?? new { };

            BitOAuth2Authenticator.State = state;

            TaskCompletionSource<(string access_token, long expires_in, string token_type)> taskSource = new TaskCompletionSource<(string access_token, long expires_in, string token_type)>();

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
                            await Logout(cancellationToken, internalAppLogoutOnly: true);
                        }
                        catch { }

                        await _accountStore.SaveAsync(e.Account, _configProvider.AppName);
                    }

                    taskSource.SetResult(await GetCurrentToken(cancellationToken));
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

        public virtual async Task<(string access_token, long expires_in, string token_type)> LoginWithCredentials(string username, string password, CancellationToken cancellationToken = default(CancellationToken))
        {
            TokenResponse tokenResponse = await _tokenClient.RequestResourceOwnerPasswordAsync(username, password, scope: "openid profile user_info");

            if (tokenResponse.IsError)
                throw new Exception($"{tokenResponse.Error}");

            try
            {
                await Logout(cancellationToken, internalAppLogoutOnly: true);
            }
            catch { }

            string[] tokenParts = tokenResponse.AccessToken.Split('.');
            byte[] decodedByteArrayToken = Convert.FromBase64String(tokenParts[1]);
            string decodedStringToken = Encoding.UTF8.GetString(decodedByteArrayToken);
            JObject jwtToken = JObject.Parse(decodedStringToken);
            string userName = jwtToken["sub"].Value<string>();

            Account account = new Account(username, new Dictionary<string, string>
            {
                {"access_token" , tokenResponse.AccessToken },
                {"expires_in" , tokenResponse.ExpiresIn.ToString()},
                {"token_type" , tokenResponse.TokenType }
            });

            await _accountStore.SaveAsync(account, _configProvider.AppName);

            return (tokenResponse.AccessToken, tokenResponse.ExpiresIn, tokenResponse.TokenType);
        }

        public virtual async Task Logout(CancellationToken cancellationToken = default(CancellationToken))
        {
            await Logout(cancellationToken, internalAppLogoutOnly: false);
        }

        private async Task Logout(CancellationToken cancellationToken, bool internalAppLogoutOnly)
        {
            Account account = await GetAccount(cancellationToken);

            if (account != null)
            {
                await _accountStore.DeleteAsync(account, _configProvider.AppName);
            }

            if (internalAppLogoutOnly == false)
            {
                WebAuthenticator.ClearCookies();
            }
        }

        public virtual async Task<(string access_token, long expires_in, string token_type)> GetCurrentToken(CancellationToken cancellationToken = default(CancellationToken))
        {
            if ((await IsLoggedIn(cancellationToken)) == false)
                throw new InvalidOperationException("Token not found");

            Account account = await GetAccount(cancellationToken);

            return (account.Properties["access_token"], Convert.ToInt64(account.Properties["expires_in"]), account.Properties["token_type"]);
        }
    }
}
