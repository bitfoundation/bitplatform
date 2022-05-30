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
                        $@"^(http|https):\/\/(\S+\.)?(bitplatform.dev|localhost|127.0.0.1|a7cd16d5.ngrok.io)(:\d+)?\b{activeAppEnvironment.GetHostVirtualPath()}\bSignIn\/?",
                        "test-oauth://"
                    },
                    PostLogoutRedirectUris = new List<string>
                    {
                        $@"^(http|https):\/\/(\S+\.)?(bitplatform.dev|localhost|127.0.0.1)(:\d+)?\b{activeAppEnvironment.GetHostVirtualPath()}\bSignOut\/?",
                        "test-oauth://"
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
