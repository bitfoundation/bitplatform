using Bit.Core.Contracts;
using System;
using System.Collections.Generic;

namespace Bit.Owin.Implementations
{
    public class CompositeDependenciesManagerProvider : IDependenciesManagerProvider, IDependenciesManager
    {
        private readonly IDependenciesManagerProvider[] _dependenciesManagerProviders;

        public CompositeDependenciesManagerProvider(params IDependenciesManagerProvider[] dependenciesManagerProviders)
        {
            if (dependenciesManagerProviders == null)
                throw new ArgumentNullException(nameof(dependenciesManagerProviders));

            _dependenciesManagerProviders = dependenciesManagerProviders;
        }

        public virtual IEnumerable<IDependenciesManager> GetDependenciesManagers()
        {
            foreach (IDependenciesManagerProvider dependencyManagerProvider in _dependenciesManagerProviders)
            {
                foreach (IDependenciesManager dependencyManager in dependencyManagerProvider.GetDependenciesManagers())
                {
                    yield return dependencyManager;
                }
            }
        }
    }
}
