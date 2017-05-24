using System.Collections.Generic;
using IdentityServer3.Core.Models;
using IdentityServer.Api.Contracts;

namespace IdentityServer.Api.Implementations
{
    public class DefaultScopesProvider : IScopesProvider
    {
        private Scope[] _result;

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
                        new ScopeClaim("primary_sid", true)
                        {
                            Description = "Primary SID"
                        },
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