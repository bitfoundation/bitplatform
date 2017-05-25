using System.Collections.Generic;
using IdentityServer3.Core.Models;

namespace Bit.IdentityServer.Contracts
{
    public interface IClientProvider
    {
        IEnumerable<Client> GetClients();
    }
}