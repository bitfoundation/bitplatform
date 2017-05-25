namespace Bit.Core.Contracts.Project
{
    public interface IDependenciesManager
    {
        void ConfigureDependencies(IDependencyManager dependencyManager);
    }
}