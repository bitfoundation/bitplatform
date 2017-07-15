using System.Collections.Generic;
using IdentityServer3.Core.Models;
using System;

namespace Bit.IdentityServer.Contracts
{
    public class BitClient
    {
        public string ClientName { get; set; }

        public string ClientId { get; set; }

        public string Secret { get; set; }

        public TimeSpan TokensLifetime { get; set; }
    }

    public class BitImplicitFlowClient : BitClient
    {
        public List<string> RedirectUris { get; set; }

        public List<string> PostLogoutRedirectUris { get; set; }
    }

    public class BitResourceOwnerFlowClient : BitClient
    {

    }

    public interface IClientProvider
    {
        IEnumerable<Client> GetClients();
    }
}