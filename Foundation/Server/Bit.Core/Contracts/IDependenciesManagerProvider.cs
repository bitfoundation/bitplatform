using System.Collections.Generic;
using Foundation.Core.Contracts.Project;

namespace Foundation.Core.Contracts
{
    public interface IDependenciesManagerProvider
    {
        IEnumerable<IDependenciesManager> GetDependenciesManagers();
    }
}