using Bit.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.OwinCore.Contracts
{
    public interface IAspNetCoreDependenciesManager : IDependenciesManager
    {
        void ConfigureServices(IServiceCollection services, IDependencyManager dependencyManager);
    }
}
