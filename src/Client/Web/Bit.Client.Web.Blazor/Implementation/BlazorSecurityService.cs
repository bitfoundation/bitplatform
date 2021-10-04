using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Http.Contracts;
using Bit.Http.Implementations;
using Newtonsoft.Json;

namespace Bit.Client.Web.Blazor.Implementation
{
    public class BlazorSecurityService : DefaultSecurityService
    {
        public WebPreferences WebPreferences { get; set; } = default!;

        public override Token? GetCurrentToken()
        {
            string? token = null;

            if (UseSecureStorage())
                throw new NotImplementedException();
            else
                token = WebPreferences.Get("Token", (string?)null);

            if (token == null)
                return null;

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(token);
        }

        public async override Task<Token?> GetCurrentTokenAsync(CancellationToken cancellationToken = default)
        {
            string? token = null;

            if (UseSecureStorage())
                throw new NotImplementedException();
            else
                token = WebPreferences.Get("Token", (string?)null);

            if (token == null)
                return null;

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(token);
        }

        public async override Task Logout(object? state = null, string? client_id = null, CancellationToken cancellationToken = default)
        {
            Token? token = await GetCurrentTokenAsync(cancellationToken).ConfigureAwait(false);

            if (token != null)
            {
                if (UseSecureStorage())
                    throw new NotImplementedException();
                else
                    WebPreferences.Remove("Token");

                TelemetryServices.All().SetUserId(null);

                if (!string.IsNullOrEmpty(token.IdToken))
                {
                    throw new NotImplementedException();
                }
            }
        }

        public override Task<Token> Login(object? state = null, string? client_id = null, IDictionary<string, string?>? acr_values = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        protected async override Task StoreToken(string jsonToken, CancellationToken cancellationToken)
        {
            if (UseSecureStorage())
            {
                throw new NotImplementedException();
            }
            else
            {
                WebPreferences.Set("Token", jsonToken);
            }

            TelemetryServices.All().SetUserId((await GetBitJwtTokenAsync(cancellationToken).ConfigureAwait(false)).UserId);
        }
    }
}
