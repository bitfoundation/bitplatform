using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using System;

namespace Autofac
{
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder RegisterDefaultSyncService(this ContainerBuilder containerBuilder, Action<ISyncService> configureDtoSetSyncConfigs)
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            containerBuilder.RegisterType<DefaultSyncService>().As<ISyncService>()
                .PropertiesAutowired(PropertyWiringOptions.PreserveSetValues)
                .OnActivated(config =>
                {
                    configureDtoSetSyncConfigs?.Invoke(config.Instance);
                }).SingleInstance().PreserveExistingDefaults();

            return containerBuilder;
        }
    }
}
