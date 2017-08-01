using System.Collections.Generic;

namespace Bit.Core.Contracts
{
    /// <summary>
    /// You've to return either one or multiple implementations of <see cref="IDependenciesManager"/> in <see cref="GetDependenciesManagers"/> method />
    /// </summary>
    public interface IDependenciesManagerProvider
    {
        IEnumerable<IDependenciesManager> GetDependenciesManagers();
    }
}