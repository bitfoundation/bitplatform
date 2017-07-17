using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Bit.Owin.Implementations;
using Microsoft.Owin.BuilderProperties;
using Microsoft.Owin.Logging;
using Owin;
using Microsoft.Owin.Security.DataProtection;

namespace Bit.Owin
{
    public class OwinAppStartup
    {
        public virtual void Configuration(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            CultureInfo culture = new CultureInfo("en-US");

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            IAppEnvironmentProvider appEnvironmentProvider = DefaultAppEnvironmentProvider.Current;

            AppEnvironment activeEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();

            AppProperties owinAppProps = new AppProperties(owinApp.Properties);

            if (activeEnvironment.DebugMode)
                owinApp.Properties["host.AppMode"] = "development";
            else
                owinApp.Properties["host.AppMode"] = "production";

            owinAppProps.AppName = activeEnvironment.AppInfo.Name;

            if (DefaultDependencyManager.Current.IsInited() == false)
            {
                DefaultDependencyManager.Current.Init();

                foreach (IOwinDependenciesManager projectDependenciesManager in DefaultDependenciesManagerProvider.Current.GetDependenciesManagers().OfType<IOwinDependenciesManager>())
                {
                    projectDependenciesManager.ConfigureDependencies(DefaultDependencyManager.Current);
                }

                DefaultDependencyManager.Current.BuildContainer();
            }

            owinApp.SetLoggerFactory(DefaultDependencyManager.Current.Resolve<ILoggerFactory>());

            if (DefaultDependencyManager.Current.IsRegistered<IDataProtectionProvider>())
                owinApp.SetDataProtectionProvider(DefaultDependencyManager.Current.Resolve<IDataProtectionProvider>());

            DefaultDependencyManager.Current.ResolveAll<IAppEvents>()
                .ToList()
                .ForEach(appEvents => appEvents.OnAppStartup());

            DefaultDependencyManager.Current.ResolveAll<IOwinMiddlewareConfiguration>()
                .ToList()
                .ForEach(middlewareConfig => middlewareConfig.Configure(owinApp));

            if (owinAppProps.OnAppDisposing != CancellationToken.None)
            {
                owinAppProps.OnAppDisposing.Register(() =>
                {
                    try
                    {
                        DefaultDependencyManager.Current.ResolveAll<IAppEvents>()
                            .ToList()
                            .ForEach(appEvents => appEvents.OnAppEnd());
                    }
                    finally
                    {
                        DefaultDependencyManager.Current.Dispose();
                    }
                });
            }
            else
            {
                throw new InvalidOperationException("owinAppProps.OnAppDisposing is not provided");
            }
        }
    }
}