using System.Collections.Generic;
using IdentityServer3.Core.Models;
using System;

namespace Bit.IdentityServer.Contracts
{
    public abstract class BitOAuthClient
    {
        public virtual string ClientName { get; set; } = default!;

        public virtual string ClientId { get; set; } = default!;

        public virtual string Secret { get; set; } = default!;

        public virtual TimeSpan TokensLifetime { get; set; } = TimeSpan.FromDays(7);

        public virtual bool Enabled { get; set; } = true;

        public override string ToString()
        {
            return $"{nameof(ClientId)}: {ClientId}, {nameof(ClientName)}: {ClientName}";
        }
    }

    public class BitImplicitFlowClient : BitOAuthClient
    {
        public virtual IEnumerable<string> RedirectUris { get; set; } = default!;

        public virtual IEnumerable<string> PostLogoutRedirectUris { get; set; } = default!;
    }

    public class BitResourceOwnerFlowClient : BitOAuthClient
    {

    }

    public interface IOAuthClientsProvider
    {
        IEnumerable<Client> GetClients();
    }
}