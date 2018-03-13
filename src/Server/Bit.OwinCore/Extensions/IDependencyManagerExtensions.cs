using Bit.Core.Extensions;
using Bit.Owin.Contracts;
using Bit.Owin.Implementations;
using Bit.Owin.Middlewares;
using Bit.OwinCore.Contracts;
using Bit.OwinCore.Implementations;
using Bit.OwinCore.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Owin.Security.DataProtection;
using Owin;
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

        /// <summary>
        /// Adds minimal asp.net core middlewares for dependency injection, exception handling and logging. It registers following asp.net core middlewares <see cref="AspNetCoreAutofacDependencyInjectionMiddlewareConfiguration"/>
        /// | <see cref="AspNetCoreExceptionHandlerMiddlewareConfiguration"/>
        /// | <see cref="AspNetCoreLogRequestInformationMiddlewareConfiguration"/>
        /// </summary>
        public static IDependencyManager RegisterMinimalAspNetCoreMiddlewares(this IDependencyManager dependencyManager)
        {
            dependencyManager.RegisterAspNetCoreMiddlewareUsing(aspNetCoreApp =>
            {
                aspNetCoreApp.UseMiddleware<AddAcceptCharsetToRequestHeadersIfNotAnyAspNetCoreMiddleware>();
            });
            dependencyManager.RegisterOwinMiddleware<AspNetCoreAutofacDependencyInjectionMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreExceptionHandlerMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreLogRequestInformationMiddlewareConfiguration>();
            return dependencyManager;
        }

        /// <summary>
        /// Configures asp.net core into your app. It registers <see cref="Microsoft.Owin.Logging.ILoggerFactory"/> by <see cref="DefaultOwinLoggerFactory"/>
        /// | <see cref="IUserInformationProvider"/> by <see cref="DefaultUserInformationProvider"/>
        /// | <see cref="IExceptionToHttpErrorMapper"/> by <see cref="DefaultExceptionToHttpErrorMapper"/>
        /// | <see cref="ITimeZoneManager"/> by <see cref="DefaultTimeZoneManager"/>
        /// | <see cref="IRequestInformationProvider"/> by <see cref="AspNetCoreRequestInformationProvider"/>
        /// | On Mono, it registers <see cref="IDataProtectionProvider"/> by <see cref="BasicDataProtectionProvider"/>
        /// </summary>
        public static IDependencyManager RegisterDefaultAspNetCoreApp(this IDependencyManager dependencyManager)
        {
            dependencyManager.Register<Microsoft.Owin.Logging.ILoggerFactory, DefaultOwinLoggerFactory>(overwriteExciting: false);
            dependencyManager.Register<IUserInformationProvider, DefaultUserInformationProvider>(overwriteExciting: false);
            dependencyManager.Register<IExceptionToHttpErrorMapper, DefaultExceptionToHttpErrorMapper>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<ITimeZoneManager, DefaultTimeZoneManager>(overwriteExciting: false);
            dependencyManager.Register<IRequestInformationProvider, AspNetCoreRequestInformationProvider>(overwriteExciting: false);
            if (PlatformUtilities.IsRunningOnMono || PlatformUtilities.IsRunningOnDotNetCore)
                dependencyManager.Register<IDataProtectionProvider, BasicDataProtectionProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.Register<IClientProfileModelProvider, DefaultClientProfileModelProvider>(overwriteExciting: false);
            dependencyManager.Register<IHtmlPageProvider, DefaultHtmlPageProvider>(overwriteExciting: false);
            return dependencyManager;
        }
    }
}
