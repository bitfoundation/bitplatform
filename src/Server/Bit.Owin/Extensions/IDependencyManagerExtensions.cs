using Bit.Core.Implementations;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Bit.Owin.Contracts.Metadata;
using Bit.Owin.Implementations;
using Bit.Owin.Implementations.Metadata;
using Bit.Owin.Middlewares;
using Owin;
using System;
using System.Linq;
using System.Reflection;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterOwinMiddleware<TMiddleware>(this IDependencyManager dependencyManager, string name = null)
            where TMiddleware : class, IOwinMiddlewareConfiguration
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IOwinMiddlewareConfiguration, TMiddleware>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false, name: name);

            return dependencyManager;
        }

        public static IDependencyManager RegisterOwinMiddlewareUsing(this IDependencyManager dependencyManager, Action<IAppBuilder> owinAppCustomizer)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (owinAppCustomizer == null)
                throw new ArgumentNullException(nameof(owinAppCustomizer));

            dependencyManager.RegisterInstance<IOwinMiddlewareConfiguration>(new DelegateOwinMiddlewareConfiguration(owinAppCustomizer), overwriteExciting: false);

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

            dependencyManager.RegisterInstance(DefaultAppModulesProvider.Current, overwriteExciting: false)
                .RegisterInstance(DefaultDependencyManager.Current, overwriteExciting: false);

            AppEnvironment RegisterAppEnvironment(IDependencyResolver resolver)
            {
                IAppEnvironmentsProvider appEnvironmentsProvider = resolver.Resolve<IAppEnvironmentsProvider>();
                return appEnvironmentsProvider.GetActiveAppEnvironment();
            }

            dependencyManager.RegisterUsing(RegisterAppEnvironment, lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.Register<IDateTimeProvider, DefaultDateTimeProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.RegisterInstance(DefaultAppEnvironmentsProvider.Current, overwriteExciting: false);
            dependencyManager.RegisterInstance(DefaultJsonContentFormatter.Current, overwriteExciting: false);
            dependencyManager.RegisterInstance(DefaultPathProvider.Current, overwriteExciting: false);
            dependencyManager.Register<IUrlStateProvider, DefaultUrlStateProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.Register<IScopeStatusManager, DefaultScopeStatusManager>(overwriteExciting: false);

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


            dependencyManager.RegisterOwinMiddleware<ClientAppProfileMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<IndexPageMiddlewareConfiguration>();

            return dependencyManager;
        }

        public static IDependencyManager RegisterMetadata(this IDependencyManager dependencyManager, params Assembly[] metadataAssemblies)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (metadataAssemblies == null)
                throw new ArgumentNullException(nameof(metadataAssemblies));

            dependencyManager.RegisterOwinMiddleware<MetadataMiddlewareConfiguration>();
            dependencyManager.Register<IAppMetadataProvider, DefaultAppMetadataProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            metadataAssemblies = AssemblyContainer.Current.AssembliesWithDefaultAssemblies(metadataAssemblies).Union(new[] { AssemblyContainer.Current.GetBitMetadataAssembly() }).ToArray();

            metadataAssemblies.SelectMany(asm => asm.GetLoadableExportedTypes())
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType)
                .Where(t => typeof(IMetadataBuilder).GetTypeInfo().IsAssignableFrom(t.GetTypeInfo()))
                .ToList()
                .ForEach(t =>
                {
                    dependencyManager.Register(typeof(IMetadataBuilder).GetTypeInfo(), t.GetTypeInfo(), lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
                });

            return dependencyManager;
        }

        public static IDependencyManager RegisterSingleSignOnClient(this IDependencyManager dependencyManager)
        {
            dependencyManager.RegisterOwinMiddleware<ReadAuthTokenFromCookieMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<SingleSignOnClientMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<SignOutPageMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<InvokeLogOutMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<SignInPageMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<InvokeLoginMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<LogUserInformationMiddlewareConfiguration>();
            dependencyManager.Register<IRandomStringProvider, DefaultRandomStringProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<IAppCertificatesProvider, DefaultAppCertificatesProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            return dependencyManager;
        }

        public static IDependencyManager RegisterLogStore<TLogStore>(this IDependencyManager dependencyManager)
            where TLogStore : class, ILogStore
        {
            dependencyManager.RegisterLogStore(typeof(TLogStore).GetTypeInfo());
            return dependencyManager;
        }

        public static IDependencyManager RegisterLogStore(this IDependencyManager dependencyManager, TypeInfo logStore)
        {
            dependencyManager.Register(typeof(ILogStore).GetTypeInfo(), logStore, overwriteExciting: false);
            return dependencyManager;
        }

        /// <summary>
        /// Configures owin into your app. It registers <see cref="Microsoft.Owin.Logging.ILoggerFactory"/> by <see cref="DefaultOwinLoggerFactory"/>
        /// | <see cref="IUserInformationProvider"/> by <see cref="DefaultUserInformationProvider"/>
        /// | <see cref="IExceptionToHttpErrorMapper"/> by <see cref="DefaultExceptionToHttpErrorMapper"/>
        /// | <see cref="ITimeZoneManager"/> by <see cref="DefaultTimeZoneManager"/>
        /// | <see cref="IRequestInformationProvider"/> by <see cref="OwinRequestInformationProvider"/>
        /// </summary>
        public static IDependencyManager RegisterDefaultOwinApp(this IDependencyManager dependencyManager)
        {
            dependencyManager.Register<Microsoft.Owin.Logging.ILoggerFactory, DefaultOwinLoggerFactory>(overwriteExciting: false);
            dependencyManager.Register<IUserInformationProvider, DefaultUserInformationProvider>(overwriteExciting: false);
            dependencyManager.Register<IExceptionToHttpErrorMapper, DefaultExceptionToHttpErrorMapper>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<ITimeZoneManager, DefaultTimeZoneManager>(overwriteExciting: false);
            dependencyManager.Register<IRequestInformationProvider, OwinRequestInformationProvider>(overwriteExciting: false);
            dependencyManager.Register<IClientProfileModelProvider, DefaultClientProfileModelProvider>(overwriteExciting: false);
            dependencyManager.Register<IHtmlPageProvider, DefaultHtmlPageProvider>(overwriteExciting: false);
#if DotNet
            dependencyManager.Register<IRouteValuesProvider, AspNetRouteValuesProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
#endif

            return dependencyManager;
        }

        /// <summary>
        /// Configures bit logging. It registers <see cref="Microsoft.Owin.Logging.ILoggerFactory"/> by <see cref="DefaultOwinLoggerFactory"/>
        /// | <see cref="ILogger"/> by <see cref="DefaultLogger"/>
        /// </summary>
        /// <param name="logStores">Class types which implemented <see cref="ILogStore"/></param>
        public static IDependencyManager RegisterDefaultLogger(this IDependencyManager dependencyManager, params TypeInfo[] logStores)
        {
            dependencyManager.Register<Microsoft.Owin.Logging.ILoggerFactory, DefaultOwinLoggerFactory>();
            dependencyManager.Register<ILogger, DefaultLogger>();

            foreach (TypeInfo logStore in logStores)
            {
                dependencyManager.RegisterLogStore(logStore);
            }

            return dependencyManager;
        }

        /// <summary>
        /// Adds minimal owin middlewares for dependency injection, exception handling and logging. It registers following owin middlewares <see cref="AutofacDependencyInjectionMiddlewareConfiguration"/>
        /// | <see cref="OwinExceptionHandlerMiddlewareConfiguration"/>
        /// | <see cref="LogRequestInformationMiddlewareConfiguration"/>
        /// </summary>
        public static IDependencyManager RegisterMinimalOwinMiddlewares(this IDependencyManager dependencyManager)
        {
            dependencyManager.RegisterOwinMiddleware<AutofacDependencyInjectionMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<OwinExceptionHandlerMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<LogRequestInformationMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddlewareUsing(owinApp =>
            {
                owinApp.Use<AddAcceptCharsetToRequestHeadersIfNotAnyMiddleware>();
            });
            return dependencyManager;
        }
    }
}
