using IdentityServer3.Core.Models;
using System.Collections.Generic;

namespace IdentityServer.Api.Contracts
{
    public interface IClientProvider
    {
        IEnumerable<Client> GetClients();
    }
}