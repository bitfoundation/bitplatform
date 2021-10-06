#if Android
using Android.App;
using Android.Content;
using System;
#endif
using Autofac;
using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Prism;
using Prism.Autofac;
using Prism.Ioc;

namespace Bit.ViewModel.Implementations
{
    public class BitPlatformInitializer : IPlatformInitializer
    {
#if Android
        private readonly Activity _activity;

        public BitPlatformInitializer(Activity activity)
        {
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));

            _activity = activity;
        }
#endif

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            ContainerBuilder containerBuilder = containerRegistry.GetBuilder();

            AutofacDependencyManager? dependencyManager;
            if (containerBuilder.Properties.TryGetValue(nameof(dependencyManager), out object? dependencyManagerObj))
            {
                dependencyManager = (AutofacDependencyManager)dependencyManagerObj!;
            }
            else
            {
                dependencyManager = new AutofacDependencyManager();
                dependencyManager.UseContainerBuilder(containerBuilder);
                containerBuilder.Properties[nameof(dependencyManager)] = dependencyManager;
            }

            IServiceCollection? services;
            if (containerBuilder.Properties.TryGetValue(nameof(services), out object? servicesObj))
            {
                services = (IServiceCollection)servicesObj!;
            }
            else
            {
                services = new BitServiceCollection();
                containerBuilder.Properties[nameof(services)] = services;
                ((IServiceCollectionAccessor)dependencyManager).ServiceCollection = services;
            }

            containerBuilder.Properties[nameof(containerRegistry)] = containerRegistry;

#if Android
            containerBuilder.Register(c => (Activity)_activity).SingleInstance().PreserveExistingDefaults();
            containerBuilder.Register(c => (Context)_activity).SingleInstance().PreserveExistingDefaults();
#endif

            RegisterTypes(dependencyManager, containerRegistry, containerBuilder, services);
        }

        public virtual void RegisterTypes(IDependencyManager dependencyManager, IContainerRegistry containerRegistry, ContainerBuilder containerBuilder, IServiceCollection services)
        {

        }
    }
}
