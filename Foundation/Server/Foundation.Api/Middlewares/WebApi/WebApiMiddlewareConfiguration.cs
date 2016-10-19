using Foundation.Api.Contracts;
using Foundation.Api.Middlewares.WebApi.Contracts;
using Foundation.Core.Contracts;
using Foundation.Core.Models;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.OData.Extensions;

namespace Foundation.Api.Middlewares.WebApi
{
    public class WebApiMiddlewareConfiguration : IOwinMiddlewareConfiguration, IDisposable
    {
        private readonly AppEnvironment _activeAppEnvironment;
        private readonly IEnumerable<IWebApiGlobalActionFiltersProvider> _globalActionFilterProviders;
        private readonly System.Web.Http.Dependencies.IDependencyResolver _webApiDependencyResolver;
        private readonly IWebApiOwinPipelineInjector _webApiOwinPipelineInjector;

        protected WebApiMiddlewareConfiguration()
        {

        }

        public WebApiMiddlewareConfiguration(IAppEnvironmentProvider appEnvironmentProvider,
            IEnumerable<IWebApiGlobalActionFiltersProvider> globalActionFilterProviders, System.Web.Http.Dependencies.IDependencyResolver webApiDependencyResolver, IWebApiOwinPipelineInjector webApiOwinPipelineInjector)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            if (globalActionFilterProviders == null)
                throw new ArgumentNullException(nameof(globalActionFilterProviders));

            if (webApiDependencyResolver == null)
                throw new ArgumentNullException(nameof(webApiDependencyResolver));

            if (webApiOwinPipelineInjector == null)
                throw new ArgumentNullException(nameof(webApiOwinPipelineInjector));

            _activeAppEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();
            _globalActionFilterProviders = globalActionFilterProviders;
            _webApiDependencyResolver = webApiDependencyResolver;
            _webApiOwinPipelineInjector = webApiOwinPipelineInjector;
        }

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            HttpConfiguration webApiConfig = new HttpConfiguration();

            _globalActionFilterProviders.ToList()
                .ForEach(actionFilterProvider =>
                {
                    actionFilterProvider.ConfigureGlobalActionFilter(webApiConfig);
                });

            webApiConfig.SetTimeZoneInfo(TimeZoneInfo.Utc);

            webApiConfig.IncludeErrorDetailPolicy = _activeAppEnvironment.DebugMode ? IncludeErrorDetailPolicy.LocalOnly : IncludeErrorDetailPolicy.Never;

            webApiConfig.DependencyResolver = _webApiDependencyResolver;

            HttpServer server = new HttpServer(webApiConfig);

            webApiConfig.MapHttpAttributeRoutes();

            webApiConfig.Routes.MapHttpRoute(name: "default", routeTemplate: "api/{controller}/{action}", defaults: new { action = RouteParameter.Optional });

            owinApp.UseAutofacWebApi(webApiConfig);

            _webApiOwinPipelineInjector.UseWebApiOData(owinApp, server);

            webApiConfig.EnsureInitialized();
        }

        public virtual void Dispose()
        {

        }
    }
}