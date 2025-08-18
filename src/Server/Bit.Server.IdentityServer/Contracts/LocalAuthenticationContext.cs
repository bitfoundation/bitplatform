using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Bit.IdentityServer.Contracts
{
    public class LocalAuthenticationContext
    {
        [JsonPropertyName("username")]
        public string? UserName { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("acr_values")]
        public Dictionary<string, string?> AcrValues { get; set; } = new();
    }
}
