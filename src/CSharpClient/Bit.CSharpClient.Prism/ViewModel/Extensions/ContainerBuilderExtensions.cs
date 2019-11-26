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

            containerBuilder.RegisterInstance<ITelemetryService>(ApplicationInsightsTelemetryService.Current);
            containerBuilder.RegisterInstance<ITelemetryService>(AppCenterTelemetryService.Current);

            containerBuilder.RegisterBuildCallback(container =>
            {
                IMessageReceiver messageReceiver = container.ResolveOptional<IMessageReceiver>();
                if (messageReceiver != null)
                {
#if UWP
                    ApplicationInsightsTelemetryService.Current.MessageReceiver = messageReceiver;
#endif
                    AppCenterTelemetryService.Current.MessageReceiver = messageReceiver;
                }
            });

            return containerBuilder;
        }
    }
}
