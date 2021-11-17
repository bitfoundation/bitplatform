using Bit.Core.Implementations;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Bit.Owin.Contracts.Metadata;
using Bit.Owin.Implementations;
using Bit.Owin.Implementations.Metadata;
using Bit.Owin.Middlewares;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin.Security.DataProtection;
using Owin;
using System;
using System.Linq;
using System.Reflection;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterAspNetCoreMiddleware<TMiddleware>(this IDependencyManager dependencyManager)
            where TMiddleware : class, IAspNetCoreMiddlewareConfiguration
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IAspNetCoreMiddlewareConfiguration, TMiddleware>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterAspNetCoreMiddlewareUsing(this IDependencyManager dependencyManager, Action<IApplicationBuilder> aspNetCoreAppCustomizer, MiddlewarePosition middlewarePosition = MiddlewarePosition.BeforeOwinMiddlewares)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (aspNetCoreAppCustomizer == null)
                throw new ArgumentNullException(nameof(aspNetCoreAppCustomizer));

            dependencyManager.RegisterInstance<IAspNetCoreMiddlewareConfiguration>(new DelegateAspNetCoreMiddlewareConfiguration(aspNetCoreAppCustomizer)
            {
                MiddlewarePosition = middlewarePosition
            }, overwriteExisting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterApplicationInsights(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            IServiceCollection services = dependencyManager.GetServiceCollection();

            services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, options) =>
            {
                module.EnableSqlCommandTextInstrumentation = true;
            });

            dependencyManager.Register<ITelemetryInitializer, BitTelemetryInitializer>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            dependencyManager.RegisterLogStore<ApplicationInsightsLogStore>();

            return dependencyManager;
        }

        /// <summary>
        /// Use app.UseRouting and app.UseCors before this and app.UseEndpoints after this.
        /// </summary>
        public static IDependencyManager RegisterAspNetCoreSingleSignOnClient(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            IServiceCollection services = dependencyManager.GetServiceCollection();

            services.AddAuthentication("JWT").AddScheme<AuthenticationSchemeOptions, BitAuthenticationHandler>("JWT", _ => { });
            services.AddAuthorization();

            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreReadAuthTokenFromCookieMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreSingleSignOnClientMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<SignOutPageMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<InvokeLogOutMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<SignInPageMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<InvokeLoginMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreLogUserInformationMiddlewareConfiguration>();
            dependencyManager.Register<IRandomStringProvider, DefaultRandomStringProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            dependencyManager.Register<IAppCertificatesProvider, DefaultAppCertificatesProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            return dependencyManager;
        }

        /// <summary>
        /// Adds minimal asp.net core middlewares for dependency injection, exception handling and logging. It registers following asp.net core middlewares <see cref="AspNetCoreAutofacDependencyInjectionMiddlewareConfiguration"/>
        /// | <see cref="AspNetCoreExceptionHandlerMiddlewareConfiguration"/>
        /// | <see cref="AspNetCoreLogRequestInformationMiddlewareConfiguration"/>
        /// </summary>
        public static IDependencyManager RegisterMinimalAspNetCoreMiddlewares(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.RegisterAspNetCoreMiddlewareUsing(aspNetCoreApp =>
            {
                aspNetCoreApp.Use(async (context, next) =>
                {
                    if (context.Request.Path != null)
                    {
                        string path = context.Request.Path.Value;

                        string[] toBeIgnoredPaths = new[] { "/core", "/signalr", "/jobs", "/api" };

                        if (toBeIgnoredPaths.Any(p => path.StartsWith(p, StringComparison.InvariantCultureIgnoreCase)) || path.EndsWith("$batch", StringComparison.InvariantCultureIgnoreCase))
                        {
                            IHttpBodyControlFeature httpBodyControlFeature = context.Features.Get<IHttpBodyControlFeature>();
                            if (httpBodyControlFeature != null)
                                httpBodyControlFeature.AllowSynchronousIO = true;
                        }
                    }

                    await next.Invoke().ConfigureAwait(false);
                });
            });

            dependencyManager.RegisterAspNetCoreMiddlewareUsing(aspNetCoreApp =>
            {
                aspNetCoreApp.UseMiddleware<AddRequiredHeadersIfNotAnyAspNetCoreMiddleware>();
            });
            dependencyManager.RegisterOwinMiddleware<AspNetCoreAutofacDependencyInjectionMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreExceptionHandlerMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<DisposePipelineAwareDisposablesMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreLogRequestInformationMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreGetRequestInfoMiddlewareConfiguration>();

            return dependencyManager;
        }

        /// <summary>
        /// Configures asp.net core into your app. It registers <see cref="Microsoft.Owin.Logging.ILoggerFactory"/> by <see cref="DefaultOwinLoggerFactory"/>
        /// | <see cref="IUserInformationProvider"/> by <see cref="DefaultUserInformationProvider"/>
        /// | <see cref="IExceptionToHttpErrorMapper"/> by <see cref="DefaultExceptionToHttpErrorMapper"/>
        /// | <see cref="ITimeZoneManager"/> by <see cref="DefaultTimeZoneManager"/>
        /// | <see cref="IRequestInformationProvider"/> by <see cref="AspNetCoreRequestInformationProvider"/>
        /// | <see cref="IRouteValuesProvider"/> by <see cref="AspNetCoreRouteValuesProvider"/>
        /// </summary>
        public static IDependencyManager RegisterDefaultAspNetCoreApp(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<Microsoft.Owin.Logging.ILoggerFactory, DefaultOwinLoggerFactory>(overwriteExisting: false);
            dependencyManager.Register<IUserInformationProvider, DefaultUserInformationProvider>(overwriteExisting: false);
            dependencyManager.Register<IExceptionToHttpErrorMapper, DefaultExceptionToHttpErrorMapper>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            dependencyManager.Register<ITimeZoneManager, DefaultTimeZoneManager>(overwriteExisting: false);
            dependencyManager.Register<IRequestInformationProvider, AspNetCoreRequestInformationProvider>(overwriteExisting: false);
            dependencyManager.Register<IDataProtectionProvider, SystemCryptoBasedDataProtectionProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            dependencyManager.Register<IClientProfileModelProvider, DefaultClientProfileModelProvider>(overwriteExisting: false);
            dependencyManager.Register<IHtmlPageProvider, DefaultHtmlPageProvider>(overwriteExisting: false);
            dependencyManager.Register<IRouteValuesProvider, AspNetCoreRouteValuesProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            return dependencyManager;
        }

        public static IDependencyManager RegisterOwinMiddleware<TMiddleware>(this IDependencyManager dependencyManager, string? name = null)
            where TMiddleware : class, IOwinMiddlewareConfiguration
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IOwinMiddlewareConfiguration, TMiddleware>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false, name: name);

            return dependencyManager;
        }

        public static IDependencyManager RegisterOwinMiddlewareUsing(this IDependencyManager dependencyManager, Action<IAppBuilder> owinAppCustomizer)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (owinAppCustomizer == null)
                throw new ArgumentNullException(nameof(owinAppCustomizer));

            dependencyManager.RegisterInstance<IOwinMiddlewareConfiguration>(new DelegateOwinMiddlewareConfiguration(owinAppCustomizer), overwriteExisting: false);

            return dependencyManager;
        }

        /// <summary>
        /// Configures minimal dependencies you need to make your app work. It registers <see cref="IDateTimeProvider"/> by <see cref="DefaultDateTimeProvider"/>
        /// | <see cref="IAppEnvironmentsProvider"/> by <see cref="DefaultAppEnvironmentsProvider"/>
        /// | <see cref="IContentFormatter"/> by <see cref="DefaultJsonContentFormatter"/>
        /// | <see cref="IPathProvider"/> by <see cref="DefaultPathProvider"/>
        /// | <see cref="IScopeStatusManager"/> by <see cref="DefaultScopeStatusManager"/>
        /// </summary>
        public static IDependencyManager RegisterMinimalDependencies(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.RegisterInstance(DefaultAppModulesProvider.Current, overwriteExisting: false)
                .RegisterInstance(DefaultDependencyManager.Current, overwriteExisting: false);

            AppEnvironment RegisterAppEnvironment(IDependencyResolver resolver)
            {
                IAppEnvironmentsProvider appEnvironmentsProvider = resolver.Resolve<IAppEnvironmentsProvider>();
                return appEnvironmentsProvider.GetActiveAppEnvironment();
            }

            dependencyManager.RegisterUsing(RegisterAppEnvironment, lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            dependencyManager.Register<IDateTimeProvider, DefaultDateTimeProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            dependencyManager.RegisterInstance(DefaultAppEnvironmentsProvider.Current, overwriteExisting: false);
            dependencyManager.RegisterInstance(DefaultJsonContentFormatter.Current, overwriteExisting: false);
            dependencyManager.RegisterInstance(DefaultPathProvider.Current, overwriteExisting: false);
            dependencyManager.Register<IUrlStateProvider, DefaultUrlStateProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            dependencyManager.Register<IScopeStatusManager, DefaultScopeStatusManager>(overwriteExisting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterSecureIndexPageMiddlewareUsingDefaultConfiguration(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.RegisterOwinMiddleware<RedirectToSsoIfNotLoggedInMiddlewareConfiguration>();

            dependencyManager.RegisterIndexPageMiddlewareUsingDefaultConfiguration();

            return dependencyManager;
        }

        public static IDependencyManager RegisterIndexPageMiddlewareUsingDefaultConfiguration(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.RegisterAspNetCoreMiddleware<IndexPageMiddlewareConfiguration>();

            return dependencyManager;
        }

        public static IDependencyManager RegisterMetadata(this IDependencyManager dependencyManager, params Assembly[] metadataAssemblies)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (metadataAssemblies == null)
                throw new ArgumentNullException(nameof(metadataAssemblies));

            dependencyManager.RegisterAspNetCoreMiddleware<MetadataMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<ClientAppProfileMiddlewareConfiguration>();
            dependencyManager.Register<IAppMetadataProvider, DefaultAppMetadataProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            metadataAssemblies = AssemblyContainer.Current.AssembliesWithDefaultAssemblies(metadataAssemblies).Union(new[] { AssemblyContainer.Current.GetServerMetadataAssembly() }).ToArray();

            metadataAssemblies.SelectMany(asm => asm.GetLoadableExportedTypes())
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType)
                .Where(t => typeof(IMetadataBuilder).GetTypeInfo().IsAssignableFrom(t.GetTypeInfo()))
                .ToList()
                .ForEach(t =>
                {
                    dependencyManager.Register(typeof(IMetadataBuilder).GetTypeInfo(), t.GetTypeInfo(), lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
                });

            return dependencyManager;
        }

        public static IDependencyManager RegisterLogStore<TLogStore>(this IDependencyManager dependencyManager)
            where TLogStore : class, ILogStore
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.RegisterLogStore(typeof(TLogStore).GetTypeInfo());
            return dependencyManager;
        }

        public static IDependencyManager RegisterLogStore(this IDependencyManager dependencyManager, TypeInfo logStore)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register(typeof(ILogStore).GetTypeInfo(), logStore, overwriteExisting: false);
            return dependencyManager;
        }

        /// <summary>
        /// Configures bit logging. It registers <see cref="Microsoft.Owin.Logging.ILoggerFactory"/> by <see cref="DefaultOwinLoggerFactory"/>
        /// | <see cref="ILogger"/> by <see cref="DefaultLogger"/>
        /// </summary>
        /// <param name="logStores">Class types which implemented <see cref="ILogStore"/></param>
        public static IDependencyManager RegisterDefaultLogger(this IDependencyManager dependencyManager, params TypeInfo[] logStores)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<Microsoft.Owin.Logging.ILoggerFactory, DefaultOwinLoggerFactory>();
            dependencyManager.Register<ILogger, DefaultLogger>();

            foreach (TypeInfo logStore in logStores)
            {
                dependencyManager.RegisterLogStore(logStore);
            }

            return dependencyManager;
        }
    }
}
