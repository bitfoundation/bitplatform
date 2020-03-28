using Bit.IdentityServer.Contracts;
using IdentityServer3.Core;
using IdentityServer3.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bit.IdentityServer.Implementations
{
    public abstract class OAuthClientsProvider : IOAuthClientsProvider
    {
        public abstract IEnumerable<Client> GetClients();

        protected virtual Client GetImplicitFlowClient(BitImplicitFlowClient client)
        {
            return new Client
            {
                ClientName = client.ClientName,
                Enabled = client.Enabled,
                ClientId = client.ClientId,
                ClientSecrets = new List<Secret>
                {
                    new Secret(client.Secret.Sha512())
                },
                Flow = Flows.Implicit,
                AllowedScopes = new List<string>
                {
                    Constants.StandardScopes.OpenId,
                    Constants.StandardScopes.Profile,
                    "user_info"
                },
                RequireConsent = false,
                RedirectUris = client.RedirectUris.ToList(),
                PostLogoutRedirectUris = client.PostLogoutRedirectUris.ToList(),
                AllowAccessToAllScopes = true,
                AlwaysSendClientClaims = true,
                IncludeJwtId = true,
                IdentityTokenLifetime = Convert.ToInt32(client.TokensLifetime.TotalSeconds),
                AccessTokenLifetime = Convert.ToInt32(client.TokensLifetime.TotalSeconds),
                AuthorizationCodeLifetime = Convert.ToInt32(client.TokensLifetime.TotalSeconds),
                AccessTokenType = AccessTokenType.Jwt,
                AllowAccessToAllCustomGrantTypes = true
            };
        }

        protected virtual Client GetResourceOwnerFlowClient(BitResourceOwnerFlowClient client)
        {
            Client result = GetImplicitFlowClient(new BitImplicitFlowClient
            {
                ClientId = client.ClientId,
                ClientName = client.ClientName,
                Enabled = client.Enabled,
                PostLogoutRedirectUris = new List<string> { },
                RedirectUris = new List<string> { },
                Secret = client.Secret,
                TokensLifetime = client.TokensLifetime
            });

            result.Flow = Flows.ResourceOwner;

            return result;
        }
    }
}
