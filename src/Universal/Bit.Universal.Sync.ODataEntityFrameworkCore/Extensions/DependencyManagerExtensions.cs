using Bit.Core.Contracts;
using Bit.Sync.ODataEntityFrameworkCore.Contracts;
using Bit.Sync.ODataEntityFrameworkCore.Implementations;
using Simple.OData.Client;
using System;

namespace Autofac
{
    public static class DependencyManagerExtensions
    {
        public static IDependencyManager RegisterDefaultSyncService(this IDependencyManager dependencyManager, Action<IServiceProvider, ISyncService> configureDtoSetSyncConfigs)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            return RegisterSyncService<DefaultSyncService>(dependencyManager, configureDtoSetSyncConfigs);
        }

        public static IDependencyManager RegisterLegacySyncService(this IDependencyManager dependencyManager, Action<IServiceProvider, ISyncService> configureDtoSetSyncConfigs)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IODataAdapterFactory, ODataAdapterFactory>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: true);
            return RegisterSyncService<DefaultSyncService>(dependencyManager, configureDtoSetSyncConfigs);
        }

        public static IDependencyManager RegisterSyncService<TSyncService>(this IDependencyManager dependencyManager, Action<IServiceProvider, ISyncService> configureDtoSetSyncConfigs)
            where TSyncService : class, ISyncService
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.GetContainerBuilder().RegisterType<TSyncService>().As<ISyncService>()
                .PropertiesAutowired(PropertyWiringOptions.PreserveSetValues)
                .OnActivated(config =>
                {
                    configureDtoSetSyncConfigs?.Invoke(config.Context.Resolve<IServiceProvider>(), config.Instance);
                }).SingleInstance().PreserveExistingDefaults();

            return dependencyManager;
        }
    }
}
