using Foundation.AspNetCore.Test.Api.Implementations.Project;
using Foundation.Core.Contracts;
using Foundation.Core.Contracts.Project;
using Foundation.Test;
using System.Collections.Generic;

namespace Foundation.AspNetCore.Test.Api.Implementations
{
    public class FoundationAspNetCoreTestServerDependenciesManagerProvider : IDependenciesManagerProvider
    {
        private readonly TestEnvironmentArgs _args;

        public FoundationAspNetCoreTestServerDependenciesManagerProvider(TestEnvironmentArgs args)
        {
            _args = args;
        }

        protected FoundationAspNetCoreTestServerDependenciesManagerProvider()
        {

        }

        public virtual IEnumerable<IDependenciesManager> GetDependenciesManagers()
        {
            return new[] { new FoundationAspNetCoreTestServerDependenciesManager(_args) };
        }
    }
}
