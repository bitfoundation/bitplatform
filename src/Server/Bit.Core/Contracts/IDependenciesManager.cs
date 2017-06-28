namespace Bit.Core.Contracts
{
    public interface IDependenciesManager
    {
        void ConfigureDependencies(IDependencyManager dependencyManager);
    }
}