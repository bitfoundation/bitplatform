using Bit.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Bit.OwinCore.Contracts
{
    /// <summary>
    /// Using <see cref="IDependencyManager" /> and <see cref="IServiceCollection"/> in <see cref="ConfigureDependencies(IServiceProvider, IServiceCollection, IDependencyManager)"/> method, you can configure middlewares such as web api and signalr + dependencies like repositories etc
    /// </summary>
    public interface IAspNetCoreDependenciesManager : IDependenciesManager
    {
        void ConfigureDependencies(IServiceProvider serviceProvider, IServiceCollection services, IDependencyManager dependencyManager);
    }
}
