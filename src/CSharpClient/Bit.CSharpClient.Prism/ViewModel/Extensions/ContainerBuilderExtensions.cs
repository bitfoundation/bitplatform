using Bit.View;
using Bit.ViewModel;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;

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

            containerBuilder.Register(context => new INavServiceFactory((prismNavService, popupNav) => DefaultNavService.INavServiceFactory<DefaultNavService>(prismNavService, popupNav))).PreserveExistingDefaults();

            containerBuilder.RegisterInstance<IExceptionHandler>(BitExceptionHandler.Current);
            containerBuilder.RegisterInstance<ITelemetryService>(ApplicationInsightsTelemetryService.Current);
            containerBuilder.RegisterInstance<ITelemetryService>(AppCenterTelemetryService.Current);
            containerBuilder.RegisterInstance<ITelemetryService>(FirebaseTelemetryService.Current);
            containerBuilder.RegisterInstance(LocalTelemetryService.Current).As<LocalTelemetryService, ITelemetryService>();
            IContainerRegistry containerRegistry = (IContainerRegistry)containerBuilder.Properties[nameof(containerRegistry)]!;
            containerRegistry.RegisterForNav<BitConsoleView, BitConsoleViewModel>("BitConsole");

            containerBuilder.RegisterBuildCallback(container =>
            {
                IMessageReceiver? messageReceiver = container.ResolveOptional<IMessageReceiver>();
                if (messageReceiver != null)
                {
                    foreach (TelemetryServiceBase telemetryService in container.Resolve<IEnumerable<ITelemetryService>>().OfType<TelemetryServiceBase>())
                    {
                        telemetryService.MessageReceiver = messageReceiver;
                    }
                }
            });

            return containerBuilder;
        }
    }
}
