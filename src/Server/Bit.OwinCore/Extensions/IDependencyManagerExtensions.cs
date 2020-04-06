using Bit.Owin.Contracts;
using Bit.Owin.Implementations;
using Bit.Owin.Middlewares;
using Bit.OwinCore.Contracts;
using Bit.OwinCore.Implementations;
using Bit.OwinCore.Middlewares;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Owin.Security.DataProtection;
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
            dependencyManager.Register<ITelemetryInitializer, BitTelemetryInitializer>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);
            dependencyManager.RegisterLogStore<ApplicationInsightsLogStore>();

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
            dependencyManager.RegisterAspNetCoreMiddlewareUsing(aspNetCoreApp =>
            {
                aspNetCoreApp.Use(async (context, next) =>
                {
                    if (context.Request.Path.HasValue)
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

                    await next.Invoke();
                });
            });

            dependencyManager.RegisterAspNetCoreMiddlewareUsing(aspNetCoreApp =>
            {
                aspNetCoreApp.UseMiddleware<AddRequiredHeadersIfNotAnyAspNetCoreMiddleware>();
            });
            dependencyManager.RegisterOwinMiddleware<AspNetCoreAutofacDependencyInjectionMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreExceptionHandlerMiddlewareConfiguration>();
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
        /// | On Mono, it registers <see cref="IDataProtectionProvider"/> by <see cref="SystemCryptoBasedDataProtectionProvider"/>
        /// </summary>
        public static IDependencyManager RegisterDefaultAspNetCoreApp(this IDependencyManager dependencyManager)
        {
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
    }
}