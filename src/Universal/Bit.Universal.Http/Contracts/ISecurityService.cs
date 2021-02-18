using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Http.Contracts
{
    public class Token
    {
        public static implicit operator Token(Dictionary<string, string> props)
        {
            if (props == null)
                throw new ArgumentNullException(nameof(props));

            Token token = new Token
            {
                AccessToken = props["access_token"],
                ExpiresIn = Convert.ToInt64(props["expires_in"], CultureInfo.InvariantCulture),
                TokenType = props["token_type"]
            };

            if (props.ContainsKey("id_token"))
                token.IdToken = props["id_token"];

            if (!props.ContainsKey("login_date"))
                token.LoginDate = DefaultDateTimeProvider.Current.GetCurrentUtcDateTime();
            else
                token.LoginDate = Convert.ToDateTime(props["login_date"], CultureInfo.InvariantCulture);

            return token;
        }

        [JsonProperty("access_token")]
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = default!;

        [JsonProperty("id_token")]
        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }

        [JsonProperty("token_type")]
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = default!;

        [JsonProperty("expires_in")]
        [JsonPropertyName("expires_in")]
        public long? ExpiresIn { get; set; }

        [JsonProperty("login_date")]
        [JsonPropertyName("login_date")]
        public DateTimeOffset? LoginDate { get; set; }

        [JsonProperty("error")]
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonProperty("error_description")]
        [JsonPropertyName("error_description")]
        public string ErrorDescription { get; set; }

        public bool IsError => !string.IsNullOrEmpty(Error) || !string.IsNullOrEmpty(ErrorDescription);
    }

    public interface ISecurityService : ISecurityServiceBase
    {
        Task<Token> LoginWithCredentials(string userName, string password, string client_id, string client_secret, string[]? scopes = null, IDictionary<string, string?>? acr_values = null, CancellationToken cancellationToken = default);

        Task<Token> Login(object? state = null, string? client_id = null, IDictionary<string, string?>? acr_values = null, CancellationToken cancellationToken = default);

        Task<Token?> GetCurrentTokenAsync(CancellationToken cancellationToken = default);

        Token? GetCurrentToken();

        Task<BitJwtToken> GetBitJwtTokenAsync(CancellationToken cancellationToken);

        BitJwtToken GetBitJwtToken();
    }
}
