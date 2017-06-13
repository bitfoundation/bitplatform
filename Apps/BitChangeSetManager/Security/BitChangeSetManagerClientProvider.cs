using IdentityServer3.Core;
using IdentityServer3.Core.Models;
using System;
using System.Collections.Generic;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.IdentityServer.Contracts;

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
                    /*  Required nuget packages: IdentityModel + Microsoft.Net.Http
                     
                        public async Task<string> CallSecureApiSample()
                        {
                            string baseAddress = "http://localhost:9090/bit-change-set-manager";

                            using (TokenClient identityClient = new TokenClient($"{baseAddress}/core/connect/token", "BitChangeSetManager-ResOwner", "secret"))
                            {
                                TokenResponse tokenResponse = await identityClient.RequestResourceOwnerPasswordAsync("test1", "test", "openid profile user_info");

                                using (HttpClient httpClient = new HttpClient { })
                                {
                                    httpClient.SetBearerToken(tokenResponse.AccessToken);

                                    string response = await httpClient.GetStringAsync($"{baseAddress}/odata/BitChangeSetManager/changeSets");

                                    return response;
                                }
                            }
                        }

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