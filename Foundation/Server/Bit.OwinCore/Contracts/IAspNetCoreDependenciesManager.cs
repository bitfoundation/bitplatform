using Bit.Core.Contracts;
using Bit.Core.Contracts.Project;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.OwinCore.Contracts
{
    public interface IAspNetCoreDependenciesManager : IDependenciesManager
    {
        void ConfigureServices(IServiceCollection services, IDependencyManager dependencyManager);
    }
}
