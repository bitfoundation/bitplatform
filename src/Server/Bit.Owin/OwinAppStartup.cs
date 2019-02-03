using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Bit.Owin.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin.BuilderProperties;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security.DataProtection;
using Owin;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Bit.Owin
{
    /// <summary>
    /// Startup class for your owin based apps. It's similar to asp.net core's startup class
    /// </summary>
    public class OwinAppStartup
    {
        /// <summary>
        /// First method called by owin hosts
        /// </summary>
        public virtual void Configuration(IAppBuilder owinApp)
        {
            if (owinApp == null)
            {
                throw new ArgumentNullException(nameof(owinApp));
            }

            CultureInfo culture = new CultureInfo("en-US");

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            AppEnvironment activeEnvironment = DefaultAppEnvironmentsProvider.Current.GetActiveAppEnvironment();

            AppProperties owinAppProps = new AppProperties(owinApp.Properties);

            if (activeEnvironment.DebugMode)
            {
                owinApp.Properties["host.AppMode"] = "development";
            }
            else
            {
                owinApp.Properties["host.AppMode"] = "production";
            }

            owinAppProps.AppName = activeEnvironment.AppInfo.Name;

            if (DefaultDependencyManager.Current.ContainerIsBuilt() == false)
            {
                DefaultDependencyManager.Current.Init();

                IServiceCollection services = new BitServiceCollection();

                if (DefaultDependencyManager.Current is IServiceCollectionAccessor dependencyManagerIServiceCollectionInterop)
                {
                    dependencyManagerIServiceCollectionInterop.ServiceCollection = services;
                }

                foreach (IAppModule appModule in DefaultAppModulesProvider.Current.GetAppModules())
                {
                    appModule.ConfigureDependencies(services, DefaultDependencyManager.Current);
                }

                DefaultDependencyManager.Current.Populate(services);

                DefaultDependencyManager.Current.BuildContainer();
            }

            owinApp.SetLoggerFactory(DefaultDependencyManager.Current.Resolve<ILoggerFactory>());

            if (DefaultDependencyManager.Current.IsRegistered<IDataProtectionProvider>())
            {
                owinApp.SetDataProtectionProvider(DefaultDependencyManager.Current.Resolve<IDataProtectionProvider>());
            }

            DefaultDependencyManager.Current.ResolveAll<IAppEvents>()
                .ToList()
                .ForEach(appEvents => appEvents.OnAppStartup());

            DefaultDependencyManager.Current.ResolveAll<IOwinMiddlewareConfiguration>()
                .ToList()
                .ForEach(middlewareConfig => middlewareConfig.Configure(owinApp));

            if (owinAppProps.OnAppDisposing != CancellationToken.None)
            {
                void OnAppDisposing()
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
                }

                owinAppProps.OnAppDisposing.Register(OnAppDisposing);
            }
            else
            {
                throw new InvalidOperationException("owinAppProps.OnAppDisposing is not provided");
            }
        }
    }

    public class BitServiceCollection : List<ServiceDescriptor>, IServiceCollection
    {

    }
}