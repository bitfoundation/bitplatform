using Foundation.Core.Contracts;
using Foundation.Core.Models;
using IdentityServer.Api.Contracts;
using IdentityServer3.Core;
using IdentityServer3.Core.Models;
using System;
using System.Collections.Generic;

namespace BitChangeSetManager.Security
{
    public class BitChangeSetManagerClientProvider : IClientProvider
    {
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;

        public BitChangeSetManagerClientProvider(IAppEnvironmentProvider appEnvironmentProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            _appEnvironmentProvider = appEnvironmentProvider;
        }

        public virtual IEnumerable<Client> GetClients()
        {
            AppEnvironment activeAppEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();

            return new[]
            {
                new Client
                {
                    ClientName = "BitChangeSetManager",
                    Enabled = true,
                    ClientId = "BitChangeSetManager",
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
                    ClientUri = "https://github.com/bit-foundation/bit-framework/",
                    RequireConsent = false,
                    RedirectUris = new List<string>
                    {
                        $@"{activeAppEnvironment.GetConfig<string>("ClientHostBaseUri")}{activeAppEnvironment.GetConfig<string>("ClientHostVirtualPath")}SignIn"
                    },
                    PostLogoutRedirectUris = new List<string>
                    {
                        $@"{activeAppEnvironment.GetConfig<string>("ClientHostBaseUri")}{activeAppEnvironment.GetConfig<string>("ClientHostVirtualPath")}SignOut"
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
                    /*  Required nuget package: IdentityModel
                        TokenClient client = new TokenClient("http://localhost:9090/bit-change-set-manager/core/connect/token", "BitChangeSetManager-ResOwner", "secret");

                        TokenResponse tokenResponse = client.RequestResourceOwnerPasswordAsync("test1", "test", "openid profile user_info").Result;

                        string access_token = tokenResponse.AccessToken;
                     */
                    ClientName = "BitChangeSetManager",
                    Enabled = true,
                    ClientId = "BitChangeSetManager-ResOwner",
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
                    ClientUri = "https://github.com/bit-foundation/bit-framework/",
                    RequireConsent = false,
                    RedirectUris = new List<string>
                    {
                        $@"{activeAppEnvironment.GetConfig<string>("ClientHostBaseUri")}{activeAppEnvironment.GetConfig<string>("ClientHostVirtualPath")}SignIn"
                    },
                    PostLogoutRedirectUris = new List<string>
                    {
                        $@"{activeAppEnvironment.GetConfig<string>("ClientHostBaseUri")}{activeAppEnvironment.GetConfig<string>("ClientHostVirtualPath")}SignOut"
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