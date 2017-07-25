using System;
using System.Linq;
using System.Reflection;
using Autofac.Integration.SignalR;
using Bit.Signalr;
using Bit.Signalr.Contracts;
using Bit.Signalr.Implementations;
using Microsoft.AspNet.SignalR;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterSignalRMiddlewareUsingDefaultConfiguration<TMessagesHubEvents>(this IDependencyManager dependencyManager, params Assembly[] hubsAssemblies)
            where TMessagesHubEvents : class, IMessagesHubEvents
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            hubsAssemblies = AssemblyContainer.Current.AssembliesWithDefaultAssemblies(hubsAssemblies).Union(new[] { AssemblyContainer.Current.GetBitSignalRAssembly() }).ToArray();

            dependencyManager.RegisterAssemblyTypes(hubsAssemblies, t => typeof(Hub).GetTypeInfo().IsAssignableFrom(t));
            dependencyManager.Register<IMessagesHubEvents, TMessagesHubEvents>(overwriteExciting: false);
            dependencyManager.Register<IMessageSender, SignalRMessageSender>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<IMessageContentFormatter, SignalRMessageContentFormatter>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<Microsoft.AspNet.SignalR.IDependencyResolver, AutofacDependencyResolver>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.RegisterInstance<Microsoft.AspNet.SignalR.Hubs.IAssemblyLocator>(new DefaultSignalRAssemblyLocator(hubsAssemblies), overwriteExciting: false);

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

        public static IDependencyManager RegisterSignalRConfigurationUsing(this IDependencyManager dependencyManager, Action<HubConfiguration> signalrHubCustomizer)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (signalrHubCustomizer == null)
                throw new ArgumentNullException(nameof(signalrHubCustomizer));

            dependencyManager.RegisterInstance<ISignalRConfiguration>(new DelegateSignalRConfiguration(signalrHubCustomizer), overwriteExciting: false);

            return dependencyManager;
        }
    }
}
