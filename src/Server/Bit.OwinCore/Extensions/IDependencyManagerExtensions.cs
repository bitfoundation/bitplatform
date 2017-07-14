using Bit.Owin.Contracts;
using Bit.Owin.Implementations;
using Bit.Owin.Middlewares;
using Bit.OwinCore.Contracts;
using Bit.OwinCore.Middlewares;
using Microsoft.AspNetCore.Builder;
using System;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterAspNetCoreMiddleware<TMiddleware>(this IDependencyManager dependencyManager)
            where TMiddleware : class, IAspNetCoreMiddlewareConfiguration
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IAspNetCoreMiddlewareConfiguration, TMiddleware>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterAspNetCoreMiddlewareUsing(this IDependencyManager dependencyManager, Action<IApplicationBuilder> aspNetCoreAppCustomizer)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (aspNetCoreAppCustomizer == null)
                throw new ArgumentNullException(nameof(aspNetCoreAppCustomizer));

            dependencyManager.RegisterInstance<IAspNetCoreMiddlewareConfiguration>(new DelegateAspNetCoreMiddlewareConfiguration(aspNetCoreAppCustomizer), overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterAspNetCoreSingleSignOnClient(this IDependencyManager dependencyManager)
        {
            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreReadAuthTokenFromCookieMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreSingleSignOnClientMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<SignOutPageMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<InvokeLogOutMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<SignInPageMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<InvokeLoginMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreLogUserInformationMiddlewareConfiguration>();
            dependencyManager.Register<IRandomStringProvider, DefaultRandomStringProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<ICertificateProvider, DefaultCertificateProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            return dependencyManager;
        }

        public static IDependencyManager RegisterMinimalOwinCoreMiddlewares(this IDependencyManager dependencyManager)
        {
            dependencyManager.RegisterOwinMiddleware<AspNetCoreAutofacDependencyInjectionMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreExceptionHandlerMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreLogRequestInformationMiddlewareConfiguration>();
            return dependencyManager;
        }

        public static IDependencyManager RegisterDefaultOwinCoreApp(this IDependencyManager dependencyManager)
        {
            dependencyManager.Register<IScopeStatusManager, DefaultScopeStatusManager>(overwriteExciting: false);
            dependencyManager.Register<Microsoft.Owin.Logging.ILoggerFactory, DefaultOwinLoggerFactory>(overwriteExciting: false);
            dependencyManager.Register<IUserInformationProvider, DefaultUserInformationProvider>(overwriteExciting: false);
            dependencyManager.Register<IExceptionToHttpErrorMapper, DefaultExceptionToHttpErrorMapper>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<ITimeZoneManager, DefaultTimeZoneManager>(overwriteExciting: false);
            dependencyManager.Register<IRequestInformationProvider, AspNetCoreRequestInformationProvider>(overwriteExciting: false);
            return dependencyManager;
        }
    }
}
