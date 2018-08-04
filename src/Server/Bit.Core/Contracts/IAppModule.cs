using Microsoft.Extensions.DependencyInjection;

namespace Bit.Core.Contracts
{
    /// <summary>
    /// To configure middlewares such as web api and signalr + dependencies like repositories etc
    /// </summary>
    public interface IAppModule
    {
        void ConfigureDependencies(IServiceCollection services, IDependencyManager dependencyManager);
    }
}
