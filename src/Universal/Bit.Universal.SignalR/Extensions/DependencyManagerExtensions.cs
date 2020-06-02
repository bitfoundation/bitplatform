using Bit.Core.Contracts;
using Bit.Signalr.Contracts;
using Bit.Signalr.Implementations;
using System;
using System.Net.Http;

namespace Autofac
{
    public static class DependencyManagerExtensions
    {
        public static IDependencyManager RegisterSignalr(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.GetContainerBuilder().Register(context => new ISignalRHttpClientFactory(httpMessageHandler => DefaultSignalRFactories.SignalRHttpClientFactory(httpMessageHandler)));
            dependencyManager.RegisterUsing(context => new IHubConnectionFactory(clientAppProfile => DefaultSignalRFactories.IHubConnectionFactory(clientAppProfile)), lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);
            dependencyManager.RegisterUsing(context => new IClientTransportFactory(signalRHttpClient => DefaultSignalRFactories.IClientTransportFactory(signalRHttpClient)), lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);

            dependencyManager.RegisterUsing(resolve =>
            {
                HttpMessageHandler authenticatedHttpMessageHandler = resolve.Resolve<HttpMessageHandler>(name: ContractKeys.AuthenticatedHttpMessageHandler);
                SignalRHttpClient signalRHttpClient = resolve.Resolve<ISignalRHttpClientFactory>()(authenticatedHttpMessageHandler);
                return signalRHttpClient;
            }, lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            dependencyManager.Register<IMessageReceiver, SignalrMessageReceiver>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            return dependencyManager;
        }
    }
}
