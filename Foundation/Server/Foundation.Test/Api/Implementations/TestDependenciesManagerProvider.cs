using System;
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

        private readonly Action<IDependencyManager> _additionalDependencies;
        private readonly bool _useSso;

        public TestDependenciesManagerProvider(Action<IDependencyManager> additionalDependencies = null, bool useSso = false)
        {
            _useSso = useSso;
            _additionalDependencies = additionalDependencies;
        }

        public virtual IEnumerable<IDependenciesManager> GetDependenciesManagers()
        {
            return new IDependenciesManager[] { new TestFoundationDependenciesManager(_useSso, _additionalDependencies) };
        }
    }
}
