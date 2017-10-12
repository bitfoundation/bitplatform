using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.IdentityServer.Contracts;
using Bit.IdentityServer.Implementations;
using IdentityServer3.Core.Models;
using System;
using System.Collections.Generic;

namespace BitChangeSetManager.Security
{
    public class BitChangeSetManagerClientProvider : ClientProvider
    {
        public virtual IAppEnvironmentProvider AppEnvironmentProvider { get; set; }

        public override IEnumerable<Client> GetClients()
        {
            AppEnvironment activeAppEnvironment = AppEnvironmentProvider.GetActiveAppEnvironment();

            return new[]
            {
                GetImplicitFlowClient(new BitImplicitFlowClient
                {
                    ClientId = "BitChangeSetManager",
                    ClientName = "BitChangeSetManager",
                    PostLogoutRedirectUris = new List<string>
                    {
                        $@"^(http|https):\/\/(\S+\.)?(bit-change-set-manager.com|localhost)(:\d+)?\b{activeAppEnvironment.GetHostVirtualPath()}\bSignOut\/?"
                    },
                    RedirectUris = new List<string>
                    {
                        $@"^(http|https):\/\/(\S+\.)?(bit-change-set-manager.com|localhost)(:\d+)?\b{activeAppEnvironment.GetHostVirtualPath()}\bSignIn\/?"
                    },
                    Secret = "secret",
                    TokensLifetime = TimeSpan.FromDays(1)
                }),
                GetResourceOwnerFlowClient(new BitResourceOwnerFlowClient
                {
                    ClientId = "BitChangeSetManager-ResOwner",
                    ClientName = "BitChangeSetManager-ResOwner",
                    Secret = "secret",
                    TokensLifetime = TimeSpan.FromDays(1)
                })

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
            };
        }
    }
}