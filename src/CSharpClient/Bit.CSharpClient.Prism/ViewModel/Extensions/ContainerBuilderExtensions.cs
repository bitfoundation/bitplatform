using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using System;

namespace Autofac
{
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder RegisterRequiredServices(this ContainerBuilder containerBuilder)
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            containerBuilder.RegisterType<DefaultDateTimeProvider>()
                .As<IDateTimeProvider>()
                .SingleInstance()
                .PreserveExistingDefaults();

            containerBuilder.RegisterType<DefaultJsonContentFormatter>()
                .As<IContentFormatter>()
                .SingleInstance()
                .PropertiesAutowired()
                .PreserveExistingDefaults();

#if UWP
            containerBuilder.RegisterInstance<ITelemetryService>(ApplicationInsightsTelementryService.Current);
#endif
            containerBuilder.RegisterInstance<ITelemetryService>(AppCenterTelementryService.Current);

            containerBuilder.RegisterBuildCallback(container =>
            {
                IMessageReceiver messageReceiver = container.ResolveOptional<IMessageReceiver>();
                if (messageReceiver != null)
                {
#if UWP
                    ApplicationInsightsTelementryService.Current.MessageReceiver = messageReceiver;
#endif
                    AppCenterTelementryService.Current.MessageReceiver = messageReceiver;
                }
            });

            return containerBuilder;
        }
    }
}
