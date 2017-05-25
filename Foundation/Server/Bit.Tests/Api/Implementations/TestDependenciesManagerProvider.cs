using System.Collections.Generic;
using Bit.Core.Contracts;
using Bit.Core.Contracts.Project;
using Bit.Test;
using Bit.Tests.Api.Implementations.Project;

namespace Bit.Tests.Api.Implementations
{
    public class TestDependenciesManagerProvider : IDependenciesManagerProvider
    {
        protected TestDependenciesManagerProvider()
        {

        }

        private readonly TestEnvironmentArgs _args;

        public TestDependenciesManagerProvider(TestEnvironmentArgs args)
        {
            _args = args;
        }

        public virtual IEnumerable<IDependenciesManager> GetDependenciesManagers()
        {
            return new IDependenciesManager[] { new TestFoundationDependenciesManager(_args) };
        }
    }
}
