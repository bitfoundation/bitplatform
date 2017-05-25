using System.Collections.Generic;
using Bit.IdentityServer.Contracts;
using IdentityServer3.Core;
using IdentityServer3.Core.Models;

namespace Bit.Tests.IdentityServer.Implementations
{
    public class TestClientProvider : IClientProvider
    {
        public virtual IEnumerable<Client> GetClients()
        {
            return new[]
            {
                new Client
                {
                    ClientName = "Test",
                    Enabled = true,
                    ClientId = "Test",
                    ClientSecrets = new List<Secret>
                    {
                        new Secret("secret".Sha512())
                    },
                    Flow = Flows.Implicit,
                    AllowedScopes = new List<string>
                    {
                        Constants.StandardScopes.OpenId,
                        Constants.StandardScopes.Profile,
                        "user_info"
                    },
                    ClientUri = "http://test.com/",
                    RequireConsent = false,
                    RedirectUris = new List<string>
                    {
                        "http://127.0.0.1/SignIn",
                        "https://127.0.0.1/SignIn"
                    },
                    PostLogoutRedirectUris = new List<string>
                    {
                        "http://127.0.0.1/SignOut",
                        "https://127.0.0.1/SignOut"
                    },
                    AllowAccessToAllScopes = true,
                    AlwaysSendClientClaims = true,
                    IncludeJwtId = true,
                    IdentityTokenLifetime = 86400 /*1 Day*/,
                    AccessTokenLifetime = 86400 /*1 Day*/,
                    AuthorizationCodeLifetime = 86400 /*1 Day*/,
                    AccessTokenType = AccessTokenType.Jwt,
                    AllowAccessToAllCustomGrantTypes = true
                },
                new Client
                {
                    ClientName = "TestResOwner",
                    Enabled = true,
                    ClientId = "TestResOwner",
                    ClientSecrets = new List<Secret>
                    {
                        new Secret("secret".Sha512())
                    },
                    Flow = Flows.ResourceOwner,
                    AllowedScopes = new List<string>
                    {
                        Constants.StandardScopes.OpenId,
                        Constants.StandardScopes.Profile,
                        "user_info"
                    },
                    ClientUri = "http://test.com/",
                    RequireConsent = false,
                    RedirectUris = new List<string>
                    {
                        "http://127.0.0.1/SignIn",
                        "https://127.0.0.1/SignIn"
                    },
                    PostLogoutRedirectUris = new List<string>
                    {
                        "http://127.0.0.1/SignOut",
                        "https://127.0.0.1/SignOut"
                    },
                    AllowAccessToAllScopes = true,
                    AlwaysSendClientClaims = true,
                    IncludeJwtId = true,
                    IdentityTokenLifetime = 86400 /*1 Day*/,
                    AccessTokenLifetime = 86400 /*1 Day*/,
                    AuthorizationCodeLifetime = 86400 /*1 Day*/,
                    AccessTokenType = AccessTokenType.Jwt,
                    AllowAccessToAllCustomGrantTypes = true
                }
            };
        }
    }
}
