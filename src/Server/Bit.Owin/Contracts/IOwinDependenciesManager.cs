using Bit.Core.Contracts;

namespace Bit.Owin.Contracts
{
    public interface IOwinDependenciesManager : IDependenciesManager
    {
        void ConfigureDependencies(IDependencyManager dependencyManager);
    }
}
