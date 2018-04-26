using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.IdentityServer.Contracts;
using Bit.IdentityServer.Implementations;
using IdentityServer3.Core.Models;
using System;
using System.Collections.Generic;

namespace Bit.Tests.IdentityServer.Implementations
{
    public class TestOAuthClientsProvider : OAuthClientsProvider
    {
        private readonly IAppEnvironmentsProvider _appEnvironmentsProvider;

        public TestOAuthClientsProvider(IAppEnvironmentsProvider appEnvironmentsProvider)
        {
            if (appEnvironmentsProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentsProvider));

            _appEnvironmentsProvider = appEnvironmentsProvider;
        }

        protected TestOAuthClientsProvider()
        {

        }

        public override IEnumerable<Client> GetClients()
        {
            AppEnvironment activeAppEnvironment = _appEnvironmentsProvider.GetActiveAppEnvironment();

            return new[]
            {
                GetImplicitFlowClient(new BitImplicitFlowClient
                {
                    ClientName = "Test",
                    ClientId = "Test",
                    Secret = "secret",
                    RedirectUris = new List<string>
                    {
                        $@"^(http|https):\/\/(\S+\.)?(bit-framework.com|localhost|127.0.0.1|indie-ir001.ngrok.io|indie-ir002.ngrok.io)(:\d+)?\b{activeAppEnvironment.GetHostVirtualPath()}\bSignIn\/?",
                        "Test://oauth2redirect"
                    },
                    PostLogoutRedirectUris = new List<string>
                    {
                        $@"^(http|https):\/\/(\S+\.)?(bit-framework.com|localhost|127.0.0.1)(:\d+)?\b{activeAppEnvironment.GetHostVirtualPath()}\bSignOut\/?",
                        "Test://oauth2redirect"
                    },
                    TokensLifetime = TimeSpan.FromDays(7),
                    Enabled = true
                }),
                GetResourceOwnerFlowClient(new BitResourceOwnerFlowClient
                {
                    ClientName = "TestResOwner",
                    ClientId = "TestResOwner",
                    Secret = "secret",
                    TokensLifetime = TimeSpan.FromDays(7),
                    Enabled = true
                })
            };
        }
    }
}
