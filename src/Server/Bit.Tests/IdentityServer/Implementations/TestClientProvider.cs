using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.IdentityServer.Contracts;
using Bit.IdentityServer.Implementations;
using IdentityServer3.Core.Models;
using System;
using System.Collections.Generic;

namespace Bit.Tests.IdentityServer.Implementations
{
    public class TestClientProvider : DefaultClientProvider
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

        public override IEnumerable<Client> GetClients()
        {
            AppEnvironment activeAppEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();

            return new[]
            {
                GetImplicitFlowClient(new BitImplicitFlowClient
                {
                    ClientName = "Test",
                    ClientId = "Test",
                    Secret = "secret",
                    RedirectUris = new List<string>
                    {
                        $@"^(http|https):\/\/(\S+\.)?(bit-framework.com|localhost|127.0.0.1)(:\d+)?\b{activeAppEnvironment.GetHostVirtualPath()}\bSignIn\/?"
                    },
                    PostLogoutRedirectUris = new List<string>
                    {
                        $@"^(http|https):\/\/(\S+\.)?(bit-framework.com|localhost|127.0.0.1)(:\d+)?\b{activeAppEnvironment.GetHostVirtualPath()}\bSignOut\/?"
                    },
                    TokensLifetime = TimeSpan.FromDays(1)
                }),
                GetResourceOwnerFlowClient(new BitResourceOwnerFlowClient
                {
                    ClientName = "TestResOwner",
                    ClientId = "TestResOwner",
                    Secret = "secret",
                    TokensLifetime = TimeSpan.FromDays(1)
                })
            };
        }
    }
}
