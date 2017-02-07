using System;
using System.Collections.Generic;
using Foundation.Core.Contracts;
using Foundation.Core.Models;
using IdentityServer3.Core;
using IdentityServer3.Core.Models;
using IdentityServer.Api.Contracts;

namespace IdentityServer.Test.Api.Implementations
{
    public class TestClientProvider : IClientProvider
    {
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;

        public TestClientProvider(IAppEnvironmentProvider appEnvironmentProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            _appEnvironmentProvider = appEnvironmentProvider;
        }

        protected TestClientProvider()
        {

        }

        public virtual IEnumerable<Client> GetClients()
        {
            AppEnvironment activeAppEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();

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
                        "http://127.0.0.1/SignIn"
                    },
                    PostLogoutRedirectUris = new List<string>
                    {
                        "http://127.0.0.1/SignOut"
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
                    ClientName = "Test2",
                    Enabled = true,
                    ClientId = "Test2",
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
                        "http://127.0.0.1/SignIn"
                    },
                    PostLogoutRedirectUris = new List<string>
                    {
                        "http://127.0.0.1/SignOut"
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
