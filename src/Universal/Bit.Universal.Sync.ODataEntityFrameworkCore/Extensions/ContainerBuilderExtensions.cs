using Bit.Core.Contracts;
using Bit.Sync.ODataEntityFrameworkCore.Contracts;
using Bit.Sync.ODataEntityFrameworkCore.Implementations;
using System;

namespace Autofac
{
    public static class ContainerBuilderExtensions
    {
        public static IDependencyManager RegisterDefaultSyncService(this IDependencyManager dependencyManager, Action<ISyncService> configureDtoSetSyncConfigs)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.GetContainerBuilder().RegisterType<DefaultSyncService>().As<ISyncService>()
                .PropertiesAutowired(PropertyWiringOptions.PreserveSetValues)
                .OnActivated(config =>
                {
                    configureDtoSetSyncConfigs?.Invoke(config.Instance);
                }).SingleInstance().PreserveExistingDefaults();

            return dependencyManager;
        }
    }
}
