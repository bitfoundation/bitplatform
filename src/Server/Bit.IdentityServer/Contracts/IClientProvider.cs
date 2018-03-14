using System.Collections.Generic;
using IdentityServer3.Core.Models;
using System;

namespace Bit.IdentityServer.Contracts
{
    public abstract class BitClient
    {
        public virtual string ClientName { get; set; }

        public virtual string ClientId { get; set; }

        public virtual string Secret { get; set; }

        public virtual TimeSpan TokensLifetime { get; set; } = TimeSpan.FromDays(7);

        public virtual bool Enabled { get; set; } = true;

        public override string ToString()
        {
            return $"{nameof(ClientId)}: {ClientId}, {nameof(ClientName)}: {ClientName}";
        }
    }

    public class BitImplicitFlowClient : BitClient
    {
        public virtual IEnumerable<string> RedirectUris { get; set; }

        public virtual IEnumerable<string> PostLogoutRedirectUris { get; set; }
    }

    public class BitResourceOwnerFlowClient : BitClient
    {

    }

    public interface IClientProvider
    {
        IEnumerable<Client> GetClients();
    }
}