using Autofac;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Exceptions;
using IdentityModel.Client;
using Newtonsoft.Json;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Bit.ViewModel.Implementations
{
    public class DefaultSecurityService : ISecurityService
    {
        public static ISecurityService Current { get; private set; }

        public DefaultSecurityService()
        {
            Current = this;
        }

        public virtual IClientAppProfile ClientAppProfile { get; set; }
        public virtual IDateTimeProvider DateTimeProvider { get; set; }
        public virtual Lazy<IContainer> ContainerProvider { get; set; }

        protected TaskCompletionSource<Token> CurrentLoginTaskCompletionSource { get; set; }
        protected string CurrentAction { get; set; }
        protected TaskCompletionSource<object> CurrentLogoutTaskCompletionSource { get; set; }

        public virtual async Task<bool> IsLoggedInAsync(CancellationToken cancellationToken = default)
        {
            Token token = await GetCurrentTokenAsync(cancellationToken).ConfigureAwait(false);

            if (token == null)
                return false;

            return (DateTimeProvider.GetCurrentUtcDateTime() - token.login_date) < TimeSpan.FromSeconds(token.expires_in);
        }

        public virtual async Task<Token> LoginWithCredentials(string userName, string password, string client_id, string client_secret, string[] scopes = null, IDictionary<string, string> acr_values = null, CancellationToken cancellationToken = default)
        {
            if (userName == null)
                throw new ArgumentNullException(nameof(userName));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (client_id == null)
                throw new ArgumentNullException(nameof(client_id));

            if (client_secret == null)
                throw new ArgumentNullException(nameof(client_secret));

            if (scopes == null)
                scopes = "openid profile user_info".Split(' ');

            HttpClient httpClient = ContainerProvider.Value.Resolve<HttpClient>();

            var parameters = new Dictionary<string, string> { };

            if (acr_values != null)
            {
                parameters.Add("acr_values", string.Join(" ", acr_values.Select(p => $"{p.Key}:{p.Value}")));
            }

            TokenResponse tokenResponse = await httpClient.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = ClientAppProfile.TokenEndpoint,
                ClientSecret = client_secret,
                ClientId = client_id,
                Scope = string.Join(" ", scopes),
                UserName = userName,
                Password = password,
                Parameters = parameters
            }, cancellationToken).ConfigureAwait(false);

            if (tokenResponse.IsError)
            {
                if (tokenResponse.Error == "invalid_grant" && !string.IsNullOrEmpty(tokenResponse.ErrorDescription))
                {
                    throw new LoginFailureException(tokenResponse.ErrorDescription, (tokenResponse.Exception ?? new Exception($"{tokenResponse.Error} {tokenResponse.Raw}")));
                }
                else
                {
                    throw tokenResponse.Exception ?? new Exception($"{tokenResponse.Error} {tokenResponse.Raw}");
                }
            }

            Token token = tokenResponse;

            string jsonToken = JsonConvert.SerializeObject(token);

            if (UseSecureStorage())
            {
                await SecureStorage.SetAsync("Token", jsonToken).ConfigureAwait(false);
            }
            else
            {
                Preferences.Set("Token", jsonToken);
            }

            return token;
        }

        public virtual async Task<Token> Login(object state = null, string client_id = null, IDictionary<string, string> acr_values = null, CancellationToken cancellationToken = default)
        {
            CurrentAction = "Login";
            CurrentLoginTaskCompletionSource = new TaskCompletionSource<Token>();
            await Browser.OpenAsync(GetLoginUrl(state, client_id, acr_values), BrowserLaunchMode.SystemPreferred).ConfigureAwait(false);
            return await CurrentLoginTaskCompletionSource.Task.ConfigureAwait(false);
        }

        public virtual async Task<Token> GetCurrentTokenAsync(CancellationToken cancellationToken = default)
        {
            string token = null;

            if (UseSecureStorage())
                token = await SecureStorage.GetAsync("Token").ConfigureAwait(false);
            else
                token = Preferences.Get("Token", (string)null);

            if (token == null)
                return null;

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(token);
        }

        public virtual async Task Logout(object state = null, string client_id = null, CancellationToken cancellationToken = default)
        {
            Token token = await GetCurrentTokenAsync(cancellationToken).ConfigureAwait(false);

            if (token != null)
            {
                if (UseSecureStorage())
                    SecureStorage.Remove("Token");
                else
                    Preferences.Remove("Token");

                if (!string.IsNullOrEmpty(token.id_token))
                {
                    CurrentAction = "Logout";
                    CurrentLogoutTaskCompletionSource = new TaskCompletionSource<object>();
                    await Browser.OpenAsync(GetLogoutUrl(token.id_token, state, client_id), BrowserLaunchMode.SystemPreferred).ConfigureAwait(false);
                    await CurrentLogoutTaskCompletionSource.Task.ConfigureAwait(false);
                }
            }
        }

        public virtual Uri GetLoginUrl(object state = null, string client_id = null, IDictionary<string, string> acr_values = null)
        {
            state = state ?? new { };

            string relativeUri = $"{ClientAppProfile.LoginEndpoint}?state={JsonConvert.SerializeObject(state)}&redirect_uri={ClientAppProfile.OAuthRedirectUri}";

            if (!string.IsNullOrEmpty(client_id))
                relativeUri += $"&client_id={client_id}";
            if (acr_values != null)
                relativeUri += $"&acr_values={string.Join(" ", acr_values.Select(p => $"{p.Key}:{p.Value}"))}";

            return new Uri(ClientAppProfile.HostUri, relativeUri: relativeUri);
        }

        public virtual Uri GetLogoutUrl(string id_token, object state = null, string client_id = null)
        {
            if (string.IsNullOrEmpty(id_token))
                throw new ArgumentException("Id token may not be empty or null", nameof(id_token));

            state = state ?? new { };

            string relativeUri = $"{ClientAppProfile.LogoutEndpint}?state={JsonConvert.SerializeObject(state)}&redirect_uri={ClientAppProfile.OAuthRedirectUri}&id_token={id_token}";

            if (!string.IsNullOrEmpty(client_id))
                relativeUri += $"&client_id={client_id}";

            return new Uri(ClientAppProfile.HostUri, relativeUri: relativeUri);
        }

        public virtual async Task OnSsoLoginLogoutRedirectCompleted(Uri url)
        {
            Dictionary<string, string> query = (Dictionary<string, string>)FormDecode(url.Fragment);

            if (CurrentAction == "Logout")
                CurrentLogoutTaskCompletionSource.SetResult(null);
            else
            {
                Token token = query;

                string jsonToken = JsonConvert.SerializeObject(token);

                if (UseSecureStorage())
                {
                    await SecureStorage.SetAsync("Token", jsonToken).ConfigureAwait(false);
                }
                else
                {
                    Preferences.Set("Token", jsonToken);
                }

                CurrentLoginTaskCompletionSource.SetResult(query);
            }
        }

        readonly char[] AmpersandChars = new char[] { '&' };

        IDictionary<string, string> FormDecode(string encodedString)
        {
            Dictionary<string, string> inputs = new Dictionary<string, string>();

            if (encodedString.Length > 0)
            {
                char firstChar = encodedString[0];
                if (firstChar == '?' || firstChar == '#')
                {
                    encodedString = encodedString.Substring(1);
                }

                if (encodedString.Length > 0)
                {
                    string[] parts = encodedString.Split(AmpersandChars, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string part in parts)
                    {
                        int equalsIndex = part.IndexOf('=');

                        string key;
                        string value;
                        if (equalsIndex >= 0)
                        {
                            key = Uri.UnescapeDataString(part.Substring(0, equalsIndex));
                            value = Uri.UnescapeDataString(part.Substring(equalsIndex + 1));
                        }
                        else
                        {
                            key = Uri.UnescapeDataString(part);
                            value = string.Empty;
                        }

                        inputs[key] = value;
                    }
                }
            }

            return inputs;
        }

        public virtual bool UseSecureStorage()
        {
            return true;
        }
    }
}
