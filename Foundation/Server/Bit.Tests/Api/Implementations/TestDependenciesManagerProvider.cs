using System.Collections.Generic;
using Foundation.Core.Contracts;
using Foundation.Core.Contracts.Project;
using Foundation.Test.Api.Implementations.Project;

namespace Foundation.Test.Api.Implementations
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
