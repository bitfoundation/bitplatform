using Foundation.Core.Contracts;
using Foundation.Core.Contracts.Project;
using IdentityServer.Test.Api.Implementations.Project;
using System.Collections.Generic;

namespace IdentityServer.Test.Api.Implementations
{
    public class IdentityServerTestDependenciesManagerProvider : IDependenciesManagerProvider
    {
        public virtual IEnumerable<IDependenciesManager> GetDependenciesManagers()
        {
            return new IDependenciesManager[] { new IdentityServerTestDependenciesManager() };
        }
    }
}
