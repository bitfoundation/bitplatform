using Bit.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Bit.OwinCore.Contracts
{
    public interface IAspNetCoreDependenciesManager : IDependenciesManager
    {
        void ConfigureDependencies(IServiceProvider serviceProvider, IServiceCollection services, IDependencyManager dependencyManager);
    }
}
