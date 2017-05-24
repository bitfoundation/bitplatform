using System.Collections.Generic;
using IdentityServer3.Core.Models;

namespace IdentityServer.Api.Contracts
{
    public interface IScopesProvider
    {
        IEnumerable<Scope> GetScopes();
    }
}