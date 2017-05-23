using Autofac.Integration.SignalR;
using Foundation.Api.Middlewares.SignalR;
using Foundation.Api.Middlewares.SignalR.Contracts;
using Foundation.Api.Middlewares.SignalR.Implementations;
using System;
using System.Linq;
using System.Reflection;

namespace Foundation.Core.Contracts
{
    public static class IDependencyManangerExtensions
    {
        public static IDependencyManager RegisterSignalRMiddlewareUsingDefaultConfiguration<TMessagesHubEvents>(this IDependencyManager dependencyManager, params Assembly[] hubsAssemblies)
            where TMessagesHubEvents : class, IMessagesHubEvents
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            hubsAssemblies = hubsAssemblies.Any() ? hubsAssemblies : new[] { Assembly.GetCallingAssembly(), typeof(MessagesHub).GetTypeInfo().Assembly };

            dependencyManager.RegisterHubs(hubsAssemblies);
            dependencyManager.Register<IMessagesHubEvents, TMessagesHubEvents>();
            dependencyManager.Register<IMessageSender, SignalRMessageSender>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IMessageContentFormatter, SignalRMessageContentFormatter>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<Microsoft.AspNet.SignalR.IDependencyResolver, AutofacDependencyResolver>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.RegisterInstance<Microsoft.AspNet.SignalR.Hubs.IAssemblyLocator>(new DefaultSignalRAssemblyLocator(hubsAssemblies));

            dependencyManager.RegisterOwinMiddleware<SignalRMiddlewareConfiguration>();

            return dependencyManager;
        }

        public static IDependencyManager RegisterSignalRMiddlewareUsingDefaultConfiguration(this IDependencyManager dependencyManager, params Assembly[] hubsAssemblies)
        {
            return RegisterSignalRMiddlewareUsingDefaultConfiguration<DefaultMessageHubEvents>(dependencyManager, hubsAssemblies);
        }

        public static IDependencyManager RegisterSignalRConfiguration<TSignalRConfiguration>(this IDependencyManager dependencyManager)
            where TSignalRConfiguration : class, ISignalRConfiguration
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<ISignalRConfiguration, TSignalRConfiguration>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            return dependencyManager;
        }
    }
}
