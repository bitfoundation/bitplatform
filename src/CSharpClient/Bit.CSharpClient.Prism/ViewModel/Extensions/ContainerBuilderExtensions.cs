using Autofac;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using System;

namespace Prism.Ioc
{
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder RegisterRequiredServices(this ContainerBuilder containerBuilder)
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            containerBuilder.RegisterType<DefaultDateTimeProvider>().As<IDateTimeProvider>().SingleInstance().PreserveExistingDefaults();

            return containerBuilder;
        }
    }
}
