using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoMapper;
using Bit.Model.Contracts;
using Bit.Owin.Contracts;
using Bit.Owin.Contracts.Metadata;
using Bit.Owin.Implementations;
using Bit.Owin.Middlewares;
using Owin;
using Bit.Core.Implementations;
using Bit.Owin.Implementations.Metadata;

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

        public static IDependencyManager RegisterAppEvents<TAppEvents>(this IDependencyManager dependencyManager)
            where TAppEvents : class, IAppEvents
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IAppEvents, TAppEvents>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

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

        public static IDependencyManager RegisterMinimalDependencies(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.RegisterInstance(DefaultDependenciesManagerProvider.Current, overwriteExciting: false)
                .RegisterInstance(DefaultDependencyManager.Current, overwriteExciting: false);

            dependencyManager.Register<IDateTimeProvider, DefaultDateTimeProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.RegisterInstance(DefaultAppEnvironmentProvider.Current, overwriteExciting: false);
            dependencyManager.RegisterInstance(DefaultJsonContentFormatter.Current, overwriteExciting: false);
            dependencyManager.RegisterInstance(DefaultPathProvider.Current, overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterSecureDefaultPageMiddlewareUsingDefaultConfiguration(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.RegisterOwinMiddleware<RedirectToSsoIfNotLoggedInMiddlewareConfiguration>();

            dependencyManager.RegisterDefaultPageMiddlewareUsingDefaultConfiguration();

            return dependencyManager;
        }

        public static IDependencyManager RegisterDefaultPageMiddlewareUsingDefaultConfiguration(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IDefaultHtmlPageProvider, RazorDefaultHtmlPageProvider>(overwriteExciting: false);
            dependencyManager.Register<IDefaultPageModelProvider, DefaultPageModelProvider>(overwriteExciting: false);

            dependencyManager.RegisterAppEvents<DefaultHtmlPageRazorTemplateConfiguration>();
            dependencyManager.RegisterOwinMiddleware<DefaultPageMiddlewareConfiguration>();

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

            metadataAssemblies.SelectMany(asm => asm.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType && t.IsPublic)
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
            dependencyManager.Register<ICertificateProvider, DefaultCertificateProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
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

        public static IDependencyManager RegisterDefaultOwinApp(this IDependencyManager dependencyManager)
        {
            dependencyManager.Register<IScopeStatusManager, DefaultScopeStatusManager>(overwriteExciting: false);
            dependencyManager.Register<Microsoft.Owin.Logging.ILoggerFactory, DefaultOwinLoggerFactory>(overwriteExciting: false);
            dependencyManager.Register<IUserInformationProvider, DefaultUserInformationProvider>(overwriteExciting: false);
            dependencyManager.Register<IExceptionToHttpErrorMapper, DefaultExceptionToHttpErrorMapper>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<ITimeZoneManager, DefaultTimeZoneManager>(overwriteExciting: false);
            dependencyManager.Register<IRequestInformationProvider, DefaultRequestInformationProvider>(overwriteExciting: false);

            return dependencyManager;
        }

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

        public static IDependencyManager RegisterMinimalOwinMiddlewares(this IDependencyManager dependencyManager)
        {
            dependencyManager.RegisterOwinMiddleware<AutofacDependencyInjectionMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<OwinExceptionHandlerMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<LogRequestInformationMiddlewareConfiguration>();
            return dependencyManager;
        }
    }
}
