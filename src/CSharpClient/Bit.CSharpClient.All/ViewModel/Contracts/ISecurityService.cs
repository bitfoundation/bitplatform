using Bit.ViewModel.Implementations;
using IdentityModel.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Auth;

namespace Bit.ViewModel.Contracts
{
    public class Token
    {
        public static Account FromTokenToAccount(Token token)
        {
            string[] tokenParts = token.access_token.Split('.');

            string fixedString = tokenParts[1].Trim().Replace(" ", "+");
            if (fixedString.Length % 4 > 0)
                fixedString = fixedString.PadRight(fixedString.Length + 4 - fixedString.Length % 4, '=');

            byte[] decodedByteArrayToken = Convert.FromBase64String(fixedString);

            string decodedStringToken = Encoding.UTF8.GetString(decodedByteArrayToken);
            JObject jwtToken = JObject.Parse(decodedStringToken);
            string userName = jwtToken["sub"].Value<string>();

            Dictionary<string, string> props = new Dictionary<string, string>
            {
                { "access_token" , token.access_token },
                { "expires_in" , token.expires_in.ToString()},
                { "token_type" , token.token_type },
                { "login_date", Convert.ToString(token.login_date ?? DefaultDateTimeProvider.Current.GetCurrentUtcDateTime()) }
            };

            if (!string.IsNullOrEmpty(token.id_token))
                props.Add(nameof(token.id_token), token.id_token);

            Account account = new Account(username: userName, properties: props);

            return account;
        }

        public static implicit operator Token(Account account)
        {
            return account.Properties;
        }

        public static implicit operator Token(Dictionary<string, string> props)
        {
            Token token = new Token
            {
                access_token = props[nameof(access_token)],
                expires_in = Convert.ToInt64(props[nameof(expires_in)]),
                token_type = props[nameof(token_type)]
            };

            if (props.ContainsKey(nameof(token.id_token)))
                token.id_token = props[nameof(token.id_token)];

            if (!props.ContainsKey(nameof(token.login_date)))
                token.login_date = DefaultDateTimeProvider.Current.GetCurrentUtcDateTime();
            else
                token.login_date = Convert.ToDateTime(props[nameof(login_date)]);

            return token;
        }

        public static implicit operator Token(TokenResponse tokenResponse)
        {
            return new Token
            {
                access_token = tokenResponse.AccessToken,
                expires_in = tokenResponse.ExpiresIn,
                login_date = DefaultDateTimeProvider.Current.GetCurrentUtcDateTime(),
                token_type = tokenResponse.TokenType
            };
        }

        public string access_token { get; set; }

        public string id_token { get; set; }

        public string token_type { get; set; }

        public long expires_in { get; set; }

        public DateTimeOffset? login_date { get; set; }
    }

    public interface ISecurityService
    {
        bool IsLoggedIn();

        Task<bool> IsLoggedInAsync(CancellationToken cancellationToken = default(CancellationToken));

        Task<Token> LoginWithCredentials(string username, string password, string client_id, string client_secret, string[] scopes = null, CancellationToken cancellationToken = default(CancellationToken));

        Task<Token> Login(object state = null, string client_id = null, CancellationToken cancellationToken = default(CancellationToken));

        Token GetCurrentToken();

        Task<Token> GetCurrentTokenAsync(CancellationToken cancellationToken = default(CancellationToken));

        Task Logout(object state = null, string client_id = null, CancellationToken cancellationToken = default(CancellationToken));

        Uri GetLoginUrl(object state = null, string client_id = null);

        Uri GetLogoutUrl(string id_token, object state = null, string client_id = null);

        void OnSsoLoginLogoutRedirectCompleted(Uri url);
    }
}
