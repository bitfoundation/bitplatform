using System.Collections.Generic;
using Bit.IdentityServer.Contracts;
using IdentityServer3.Core;
using IdentityServer3.Core.Models;
using Bit.Core.Models;
using Bit.Core.Contracts;
using System;

namespace Bit.Tests.IdentityServer.Implementations
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
                        $@"^(http|https):\/\/(\S+\.)?(bit-framework.com|localhost|127.0.0.1)(:\d+)?\b{activeAppEnvironment.GetHostVirtualPath()}\bSignIn\/?"
                    },
                    PostLogoutRedirectUris = new List<string>
                    {
                        $@"^(http|https):\/\/(\S+\.)?(bit-framework.com|localhost|127.0.0.1)(:\d+)?\b{activeAppEnvironment.GetHostVirtualPath()}\bSignOut\/?"
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
                    RequireConsent = false,
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
