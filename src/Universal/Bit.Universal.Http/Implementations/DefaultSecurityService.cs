﻿using Autofac;
using Bit.Core.Contracts;
using Bit.Core.Exceptions;
using Bit.Core.Implementations;
using Bit.Core.Models;
using Bit.Http.Contracts;
using Newtonsoft.Json;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials.Interfaces;

namespace Bit.Http.Implementations
{
    public class DefaultSecurityService : ISecurityService
    {
        public IClientAppProfile ClientAppProfile { get; set; } = default!;
        public IDateTimeProvider DateTimeProvider { get; set; } = default!;
        public IEnumerable<ITelemetryService> TelemetryServices { get; set; } = default!;
        public Lazy<IContainer> ContainerProvider { get; set; } = default!;
        public IWebAuthenticator WebAuthenticator { get; set; } = default!;
        public IPreferences Preferences { get; set; } = default!;
        public ISecureStorage SecureStorage { get; set; } = default!;

        public virtual async Task<bool> IsLoggedInAsync(CancellationToken cancellationToken = default)
        {
            Token? token = await GetCurrentTokenAsync(cancellationToken).ConfigureAwait(false);

            if (token == null)
                return false;

            return (DateTimeProvider.GetCurrentUtcDateTime() - token.LoginDate) < TimeSpan.FromSeconds(token.ExpiresIn.Value);
        }

        public virtual bool IsLoggedIn()
        {
            Token? token = GetCurrentToken();

            if (token == null)
                return false;

            return (DateTimeProvider.GetCurrentUtcDateTime() - token.LoginDate) < TimeSpan.FromSeconds(token.ExpiresIn.Value);
        }

        public virtual async Task<Token> LoginWithCredentials(string userName, string password, string client_id, string client_secret, string[]? scopes = null, IDictionary<string, string?>? acr_values = null, CancellationToken cancellationToken = default)
        {
            if (userName == null)
            {
                throw new ArgumentNullException(nameof(userName));
            }
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }
            if (client_id == null)
            {
                throw new ArgumentNullException(nameof(client_id));
            }
            if (client_secret == null)
            {
                throw new ArgumentNullException(nameof(client_secret));
            }

            HttpClient httpClient = ContainerProvider.Value.Resolve<HttpClient>();

            scopes = scopes ?? new[] { "openid", "profile", "user_info" };

            Dictionary<string, string> loginData = new Dictionary<string, string>
            {
                { "scope", string.Join(" ", scopes) },
                { "grant_type", "password" },
                { "username", userName },
                { "password", password },
                { "client_id", client_id },
                { "client_secret", client_secret }
            };

            if (acr_values != null)
            {
                loginData.Add("acr_values", string.Join(" ", acr_values.Select(p => $"{p.Key}:{Uri.EscapeDataString(p.Value ?? string.Empty)}")));
            }

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "core/connect/token");

            request.Content = new FormUrlEncodedContent(loginData);

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

#if DotNetStandard2_0 || UWP
            using Stream responseContent = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#elif Android || iOS || DotNetStandard2_1
            await using Stream responseContent = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#else
            await using Stream responseContent = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#endif

            Token token = await DefaultJsonContentFormatter.Current.DeserializeAsync<Token>(responseContent, cancellationToken).ConfigureAwait(false);

            if (token.IsError)
            {
                if (token.Error == "invalid_grant" && !string.IsNullOrEmpty(token.ErrorDescription))
                {
                    throw new LoginFailureException(token.ErrorDescription, new Exception(token.Error));
                }
                else
                {
                    throw new Exception(token.Error);
                }
            }

            token.LoginDate = DateTimeOffset.UtcNow;

            string jsonToken = JsonConvert.SerializeObject(token);

            await StoreToken(jsonToken, cancellationToken).ConfigureAwait(false);

            return token;
        }

        public virtual async Task<Token> Login(object? state = null, string? client_id = null, IDictionary<string, string?>? acr_values = null, CancellationToken cancellationToken = default)
        {
            var authResult = await WebAuthenticator.AuthenticateAsync(GetLoginUrl(state, client_id, acr_values), ClientAppProfile.OAuthRedirectUri).ConfigureAwait(false);

            Token token = authResult.Properties;

            string jsonToken = JsonConvert.SerializeObject(token);

            await StoreToken(jsonToken, cancellationToken).ConfigureAwait(false);

            return token;
        }

        public virtual Token? GetCurrentToken()
        {
            string? token = null;

            if (UseSecureStorage())
                token = SecureStorage.GetAsync("Token").GetAwaiter().GetResult();
            else
                token = Preferences.Get("Token", (string?)null);

            if (token == null)
                return null;

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(token);
        }

        public virtual async Task<Token?> GetCurrentTokenAsync(CancellationToken cancellationToken = default)
        {
            string? token = null;

            if (UseSecureStorage())
                token = await SecureStorage.GetAsync("Token").ConfigureAwait(false);
            else
                token = Preferences.Get("Token", (string?)null);

            if (token == null)
                return null;

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(token);
        }

        public virtual async Task Logout(object? state = null, string? client_id = null, CancellationToken cancellationToken = default)
        {
            Token? token = await GetCurrentTokenAsync(cancellationToken).ConfigureAwait(false);

            if (token != null)
            {
                if (UseSecureStorage())
                    SecureStorage.Remove("Token");
                else
                    Preferences.Remove("Token");

                TelemetryServices.All().SetUserId(null);

                if (!string.IsNullOrEmpty(token.IdToken))
                {
                    await WebAuthenticator.AuthenticateAsync(GetLogoutUrl(token.IdToken!, state, client_id), ClientAppProfile.OAuthRedirectUri).ConfigureAwait(false);
                }
            }
        }

        public virtual Uri GetLoginUrl(object? state = null, string? client_id = null, IDictionary<string, string?>? acr_values = null)
        {
            state = state ?? new { };

            string relativeUri = $"{ClientAppProfile.LoginEndpoint ?? throw new InvalidOperationException($"{nameof(IClientAppProfile.LoginEndpoint)} is null.")}?state={JsonConvert.SerializeObject(state)}&redirect_uri={ClientAppProfile.OAuthRedirectUri ?? throw new InvalidOperationException($"{nameof(IClientAppProfile.OAuthRedirectUri)} is null.")}";

            if (!string.IsNullOrEmpty(client_id))
                relativeUri += $"&client_id={client_id}";
            if (acr_values != null)
                relativeUri += $"&acr_values={string.Join(" ", acr_values.Select(p => $"{p.Key}:{Uri.EscapeDataString(p.Value ?? string.Empty)}"))}";

            return new Uri(ClientAppProfile.HostUri ?? throw new InvalidOperationException($"{nameof(IClientAppProfile.HostUri)} is null."), relativeUri: relativeUri);
        }

        public virtual Uri GetLogoutUrl(string id_token, object? state = null, string? client_id = null)
        {
            if (string.IsNullOrEmpty(id_token))
                throw new ArgumentException("Id token may not be empty or null", nameof(id_token));

            state = state ?? new { };

            string relativeUri = $"{ClientAppProfile.LogoutEndpint ?? throw new InvalidOperationException($"{nameof(IClientAppProfile.LogoutEndpint)} is null.")}?state={JsonConvert.SerializeObject(state)}&redirect_uri={ClientAppProfile.OAuthRedirectUri ?? throw new InvalidOperationException($"{nameof(IClientAppProfile.OAuthRedirectUri)} is null.")}&id_token={id_token}";

            if (!string.IsNullOrEmpty(client_id))
                relativeUri += $"&client_id={client_id}";

            return new Uri(ClientAppProfile.HostUri ?? throw new InvalidOperationException($"{nameof(IClientAppProfile.HostUri)} is null."), relativeUri: relativeUri);
        }

        protected virtual async Task StoreToken(string jsonToken, CancellationToken cancellationToken)
        {
            if (UseSecureStorage())
            {
                await SecureStorage.SetAsync("Token", jsonToken).ConfigureAwait(false);
            }
            else
            {
                Preferences.Set("Token", jsonToken);
            }

            TelemetryServices.All().SetUserId((await GetBitJwtTokenAsync(cancellationToken).ConfigureAwait(false)).UserId);
        }

        public virtual bool UseSecureStorage()
        {
            return false;
        }

        public virtual async Task<BitJwtToken> GetBitJwtTokenAsync(CancellationToken cancellationToken)
        {
            if (!await IsLoggedInAsync(cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException("User is not logged in.");

            Token token = (await GetCurrentTokenAsync(cancellationToken).ConfigureAwait(false))!;

            var handler = new JwtSecurityTokenHandler();

            var jwtToken = (JwtSecurityToken)handler.ReadToken(token.AccessToken);

            var primary_sid = jwtToken.Claims.First(c => c.Type == "primary_sid").Value;

            return BitJwtToken.FromJson(primary_sid);
        }

        public virtual BitJwtToken GetBitJwtToken()
        {
            if (!IsLoggedIn())
                throw new InvalidOperationException("User is not logged in.");

            Token token = GetCurrentToken()!;

            var handler = new JwtSecurityTokenHandler();

            var jwtToken = (JwtSecurityToken)handler.ReadToken(token.AccessToken);

            var primary_sid = jwtToken.Claims.First(c => c.Type == "primary_sid").Value;

            return BitJwtToken.FromJson(primary_sid);
        }

        public async Task<string?> GetUserIdAsync(CancellationToken cancellationToken)
        {
            return (await GetBitJwtTokenAsync(cancellationToken).ConfigureAwait(false))?.UserId;
        }
    }
}
