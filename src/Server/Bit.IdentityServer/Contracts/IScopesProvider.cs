using IdentityServer3.Core.Models;
using System.Collections.Generic;

namespace Bit.IdentityServer.Contracts
{
    public interface IScopesProvider
    {
        IEnumerable<Scope> GetScopes();
    }
}
