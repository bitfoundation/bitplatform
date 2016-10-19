using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Foundation.Api.Implementations;
using Foundation.Core.Contracts;
using Foundation.Core.Contracts.Project;
using Foundation.Core.Models;
using Owin;
using Foundation.Api.Contracts;
using Microsoft.Owin.Logging;
using Microsoft.Owin.BuilderProperties;

namespace Foundation.Api
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

            owinApp.SetLoggerFactory(new DiagnosticsLoggerFactory());

            if (DefaultDependencyManager.Current.IsInited() == false)
            {
                DefaultDependencyManager.Current.Init();

                foreach (IDependenciesManager projectDependenciesManager in DefaultDependenciesManagerProvider.Current.GetDependenciesManagers())
                {
                    projectDependenciesManager.ConfigureDependencies(DefaultDependencyManager.Current);
                }

                DefaultDependencyManager.Current.BuildContainer();
            }

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