using Bit.Core.Models;
using Bit.IdentityServer.Contracts;
using Bit.IdentityServer.Implementations;
using IdentityServer3.Core.Models;
using System;
using System.Collections.Generic;

namespace BitChangeSetManager.Security
{
    public class BitChangeSetManagerClientProvider : OAuthClientsProvider
    {
        public virtual AppEnvironment AppEnvironment { get; set; }

        public override IEnumerable<Client> GetClients()
        {
            return new[]
            {
                GetImplicitFlowClient(new BitImplicitFlowClient
                {
                    ClientId = "BitChangeSetManager",
                    ClientName = "BitChangeSetManager",
                    PostLogoutRedirectUris = new List<string>
                    {
                        $@"^(http|https):\/\/(\S+\.)?(bit-change-set-manager.com|localhost)(:\d+)?\b{AppEnvironment.GetHostVirtualPath()}\bSignOut\/?"
                    },
                    RedirectUris = new List<string>
                    {
                        $@"^(http|https):\/\/(\S+\.)?(bit-change-set-manager.com|localhost)(:\d+)?\b{AppEnvironment.GetHostVirtualPath()}\bSignIn\/?"
                    },
                    Secret = "secret",
                    TokensLifetime = TimeSpan.FromDays(7),
                    Enabled = true
                }),
                GetResourceOwnerFlowClient(new BitResourceOwnerFlowClient
                {
                    ClientId = "BitChangeSetManager-ResOwner",
                    ClientName = "BitChangeSetManager-ResOwner",
                    Secret = "secret",
                    TokensLifetime = TimeSpan.FromDays(7),
                    Enabled = true
                })

                /*  Required nuget packages: IdentityModel + Microsoft.Net.Http
                     
                public async Task<string> CallSecureApiSample()
                {
                    using (HttpClient httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:9090/") }) // Use services.AddHttpClient()
                    {
                        TokenResponse tokenResponse = await httpClient.RequestPasswordTokenAsync(new PasswordTokenRequest
                        {
                            Address = "core/connect/token",
                            ClientSecret = "secret",
                            ClientId = "BitChangeSetManager-ResOwner",
                            Scope = "openid profile user_info",
                            UserName = "test1",
                            Password = "test"
                        });

                    httpClient.SetBearerToken(tokenResponse.AccessToken);

                    string response = await httpClient.GetStringAsync($"odata/BitChangeSetManager/changeSets");

                    return response;
                }

                */
            };
        }
    }
}
