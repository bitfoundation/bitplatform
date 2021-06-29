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
using Xamarin.Essentials.Implementation;
using Xamarin.Essentials.Interfaces;

namespace Autofac
{
    public static class DependencyManagerExtensions
    {
        public static IDependencyManager RegisterRequiredServices(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.RegisterXamarinEssentials();

            dependencyManager.Register<IDateTimeProvider, DefaultDateTimeProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            dependencyManager.RegisterInstance(DefaultJsonContentFormatter.Current, overwriteExisting: false);

            dependencyManager.RegisterUsing(resolver => new INavServiceFactory((prismNavService, popupNav, regionManager) => DefaultNavService.INavServiceFactory<DefaultNavService>(prismNavService, popupNav, regionManager)), overwriteExisting: false, lifeCycle: DependencyLifeCycle.Transient);

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

        public static IDependencyManager RegisterXamarinEssentials(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IAppInfo, AppInfoImplementation>(lifeCycle: DependencyLifeCycle.PerScopeInstance, overwriteExisting: false);
            dependencyManager.Register<IClipboard, ClipboardImplementation>(lifeCycle: DependencyLifeCycle.PerScopeInstance, overwriteExisting: false);
            dependencyManager.Register<IConnectivity, ConnectivityImplementation>(lifeCycle: DependencyLifeCycle.PerScopeInstance, overwriteExisting: false);
            dependencyManager.Register<IDeviceInfo, DeviceInfoImplementation>(lifeCycle: DependencyLifeCycle.PerScopeInstance, overwriteExisting: false);
            dependencyManager.Register<IMainThread, MainThreadImplementation>(lifeCycle: DependencyLifeCycle.PerScopeInstance, overwriteExisting: false);
            dependencyManager.Register<IPreferences, PreferencesImplementation>(lifeCycle: DependencyLifeCycle.PerScopeInstance, overwriteExisting: false);
            dependencyManager.Register<ISecureStorage, SecureStorageImplementation>(lifeCycle: DependencyLifeCycle.PerScopeInstance, overwriteExisting: false);
            dependencyManager.Register<IVersionTracking, VersionTrackingImplementation>(lifeCycle: DependencyLifeCycle.PerScopeInstance, overwriteExisting: false);
            dependencyManager.Register<IWebAuthenticator, WebAuthenticatorImplementation>(lifeCycle: DependencyLifeCycle.PerScopeInstance, overwriteExisting: false);

            return dependencyManager;
        }
    }
}
