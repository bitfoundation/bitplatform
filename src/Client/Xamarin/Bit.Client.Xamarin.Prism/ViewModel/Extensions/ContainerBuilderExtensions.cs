using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.View;
using Bit.ViewModel;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Autofac
{
    public static class ContainerBuilderExtensions
    {
        public static IDependencyManager RegisterRequiredServices(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IDateTimeProvider, DefaultDateTimeProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            dependencyManager.Register<IContentFormatter, DefaultJsonContentFormatter>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            dependencyManager.RegisterUsing(resolver => new INavServiceFactory((prismNavService, popupNav) => DefaultNavService.INavServiceFactory<DefaultNavService>(prismNavService, popupNav)), overwriteExisting: false, lifeCycle: DependencyLifeCycle.Transient);

            dependencyManager.RegisterInstance<IExceptionHandler>(BitExceptionHandler.Current);
            dependencyManager.RegisterInstance<ITelemetryService>(ApplicationInsightsTelemetryService.Current);
            dependencyManager.RegisterInstance<ITelemetryService>(AppCenterTelemetryService.Current);
            dependencyManager.RegisterInstance<ITelemetryService>(FirebaseTelemetryService.Current);
            dependencyManager.RegisterInstance(LocalTelemetryService.Current, servicesType: new[] { typeof(LocalTelemetryService).GetTypeInfo(), typeof(ITelemetryService).GetTypeInfo() });
            IContainerRegistry containerRegistry = dependencyManager.GetContainerRegistry();
            containerRegistry.RegisterForNav<BitConsoleView, BitConsoleViewModel>("BitConsole");

            dependencyManager.GetContainerBuilder().RegisterBuildCallback(container =>
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

            return dependencyManager;
        }
    }
}
