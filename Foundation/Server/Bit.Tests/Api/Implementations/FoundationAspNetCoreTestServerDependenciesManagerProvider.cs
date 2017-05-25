using System.Collections.Generic;
using Bit.Core.Contracts;
using Bit.Core.Contracts.Project;
using Bit.Test;
using Bit.Tests.Api.Implementations.Project;

namespace Bit.Tests.Api.Implementations
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
