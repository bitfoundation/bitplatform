using Bit.Core.Contracts;

namespace Bit.Owin.Contracts
{
    /// <summary>
    /// Using <see cref="IDependencyManager" /> in <see cref="ConfigureDependencies(IDependencyManager)"/> method, you can configure middlewares such as web api and signalr + dependencies like repositories etc
    /// </summary>
    public interface IOwinDependenciesManager : IDependenciesManager
    {
        void ConfigureDependencies(IDependencyManager dependencyManager);
    }
}
