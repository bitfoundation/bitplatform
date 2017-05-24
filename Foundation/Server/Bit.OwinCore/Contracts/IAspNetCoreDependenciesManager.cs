using Foundation.Core.Contracts;
using Foundation.Core.Contracts.Project;
using Microsoft.Extensions.DependencyInjection;

namespace Foundation.AspNetCore.Contracts
{
    public interface IAspNetCoreDependenciesManager : IDependenciesManager
    {
        void ConfigureServices(IServiceCollection services, IDependencyManager dependencyManager);
    }
}
