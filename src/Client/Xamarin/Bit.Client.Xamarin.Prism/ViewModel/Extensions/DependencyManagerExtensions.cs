using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.View;
using Bit.ViewModel;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using Prism.Ioc;
using System;
using System.Reflection;

namespace Autofac
{
    public static class DependencyManagerExtensions
    {
        public static IDependencyManager RegisterRequiredServices(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IDateTimeProvider, DefaultDateTimeProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            dependencyManager.RegisterInstance(DefaultJsonContentFormatter.Current, overwriteExisting: false);

            dependencyManager.RegisterUsing(resolver => new AnimateNavigation(() => false), overwriteExisting: false);
            dependencyManager.RegisterUsing(resolver => new INavServiceFactory((prismNavService, popupNav, regionManager, animateNavigation) => DefaultNavService.INavServiceFactory<DefaultNavService>(prismNavService, popupNav, regionManager, animateNavigation)), overwriteExisting: false, lifeCycle: DependencyLifeCycle.Transient);

            dependencyManager.RegisterInstance<IExceptionHandler>(BitExceptionHandler.Current);

            dependencyManager.RegisterInstance<ITelemetryService>(ApplicationInsightsTelemetryService.Current);
            dependencyManager.RegisterInstance<ITelemetryService>(AppCenterTelemetryService.Current);
            dependencyManager.RegisterInstance<ITelemetryService>(FirebaseTelemetryService.Current);
            dependencyManager.RegisterInstance<ITelemetryService>(DebugTelemetryService.Current);
            dependencyManager.RegisterInstance<ITelemetryService>(ConsoleTelemetryService.Current);
            dependencyManager.RegisterInstance(LocalTelemetryService.Current, servicesType: new[] { typeof(LocalTelemetryService).GetTypeInfo(), typeof(ITelemetryService).GetTypeInfo() });
            IContainerRegistry containerRegistry = dependencyManager.GetContainerRegistry();
            containerRegistry.RegisterForNav<BitConsoleView, BitConsoleViewModel>("BitConsole");

            return dependencyManager;
        }
    }
}
