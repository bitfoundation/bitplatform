using System.Collections.Generic;

namespace Bit.Core.Contracts
{
    public interface IDependenciesManagerProvider
    {
        IEnumerable<IDependenciesManager> GetDependenciesManagers();
    }
}