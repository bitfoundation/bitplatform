using Autofac;
using Bit.Core.Implementations;
using Bit.ViewModel;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Essentials.Interfaces;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterRequiredServices(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.RegisterXamarinEssentialsImplementations();

            dependencyManager.Register<IDateTimeProvider, DefaultDateTimeProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            dependencyManager.RegisterInstance(DefaultJsonContentFormatter.Current, overwriteExisting: false);

            dependencyManager.RegisterInstance<IExceptionHandler>(BitExceptionHandler.Current);

            dependencyManager.GetContainerBuilder().RegisterBuildCallback(container =>
            {
                IMessageReceiver? messageReceiver = container.ResolveOptional<IMessageReceiver>();
                IConnectivity connectivity = container.Resolve<IConnectivity>();
                IVersionTracking versionTracking = container.Resolve<IVersionTracking>();

                foreach (TelemetryServiceBase telemetryService in container.Resolve<IEnumerable<ITelemetryService>>().OfType<TelemetryServiceBase>())
                {
                    if (messageReceiver != null)
                        telemetryService.MessageReceiver = messageReceiver;
                    telemetryService.Connectivity = connectivity;
                    telemetryService.VersionTracking = versionTracking;
                }
            });

            return dependencyManager;
        }
    }
}
