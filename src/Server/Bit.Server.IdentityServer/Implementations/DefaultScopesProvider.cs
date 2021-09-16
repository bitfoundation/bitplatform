using Bit.IdentityServer.Contracts;
using IdentityServer3.Core.Models;
using System.Collections.Generic;

namespace Bit.IdentityServer.Implementations
{
    public class DefaultScopesProvider : IScopesProvider
    {
        private Scope[]? _result;

        public virtual IEnumerable<Scope> GetScopes()
        {
            if (_result != null)
                return _result;

            _result = new[]
            {
                StandardScopes.OpenId,
                StandardScopes.Profile,
                new Scope
                {
                    Claims = new List<ScopeClaim>
                    {
                        new ScopeClaim("upn", true)
                        {
                            Description = "User Principal Name"
                        }
                    },
                    Name = "user_info",
                    DisplayName = "User Information",
                    Description = "User Information",
                    Required = true,
                    IncludeAllClaimsForUser = true,
                    ShowInDiscoveryDocument = true,
                    Enabled = true,
                    AllowUnrestrictedIntrospection = true
                }
            };

            return _result;
        }
    }
}
