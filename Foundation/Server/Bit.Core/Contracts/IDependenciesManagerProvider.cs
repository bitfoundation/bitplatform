using System.Collections.Generic;
using Bit.Core.Contracts.Project;

namespace Bit.Core.Contracts
{
    public interface IDependenciesManagerProvider
    {
        IEnumerable<IDependenciesManager> GetDependenciesManagers();
    }
}