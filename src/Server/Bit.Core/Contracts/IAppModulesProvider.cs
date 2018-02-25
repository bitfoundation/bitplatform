using System.Collections.Generic;

namespace Bit.Core.Contracts
{
    /// <summary>
    /// You've to return either one or multiple implementations of <see cref="IAppModule"/> in <see cref="GetAppModules"/> method />
    /// </summary>
    public interface IAppModulesProvider
    {
        IEnumerable<IAppModule> GetAppModules();
    }
}