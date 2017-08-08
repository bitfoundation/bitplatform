using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Bit.WebApi.Contracts;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace Bit.WebApi
{
    public class WebApiMiddlewareConfiguration : IOwinMiddlewareConfiguration, IDisposable
    {
        private readonly AppEnvironment _activeAppEnvironment;
        private readonly IEnumerable<IWebApiConfigurationCustomizer> _webApiConfgurationCustomizers;
        private readonly System.Web.Http.Dependencies.IDependencyResolver _webApiDependencyResolver;
        private readonly IWebApiOwinPipelineInjector _webApiOwinPipelineInjector;
        private HttpConfiguration _webApiConfig;
        private HttpServer _server;

#if DEBUG
        protected WebApiMiddlewareConfiguration()
        {
        }
#endif

        public WebApiMiddlewareConfiguration(IAppEnvironmentProvider appEnvironmentProvider,
            IEnumerable<IWebApiConfigurationCustomizer> webApiConfgurationCustomizers, System.Web.Http.Dependencies.IDependencyResolver webApiDependencyResolver, IWebApiOwinPipelineInjector webApiOwinPipelineInjector)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            if (webApiConfgurationCustomizers == null)
                throw new ArgumentNullException(nameof(webApiConfgurationCustomizers));

            if (webApiDependencyResolver == null)
                throw new ArgumentNullException(nameof(webApiDependencyResolver));

            if (webApiOwinPipelineInjector == null)
                throw new ArgumentNullException(nameof(webApiOwinPipelineInjector));

            _activeAppEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();
            _webApiConfgurationCustomizers = webApiConfgurationCustomizers;
            _webApiDependencyResolver = webApiDependencyResolver;
            _webApiOwinPipelineInjector = webApiOwinPipelineInjector;
        }

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            _webApiConfig = new HttpConfiguration();
            _webApiConfig.SuppressHostPrincipal();

            _webApiConfig.IncludeErrorDetailPolicy = _activeAppEnvironment.DebugMode ? IncludeErrorDetailPolicy.LocalOnly : IncludeErrorDetailPolicy.Never;

            _webApiConfig.DependencyResolver = _webApiDependencyResolver;

            _webApiConfgurationCustomizers.ToList()
                .ForEach(webApiConfigurationCustomizer =>
                {
                    webApiConfigurationCustomizer.CustomizeWebApiConfiguration(_webApiConfig);
                });

            _server = new HttpServer(_webApiConfig);

            _webApiConfig.MapHttpAttributeRoutes();

            _webApiConfig.Routes.MapHttpRoute(name: "default", routeTemplate: "{controller}/{action}", defaults: new { action = RouteParameter.Optional });

            owinApp.UseAutofacWebApi(_webApiConfig);

            _webApiOwinPipelineInjector.UseWebApiOData(owinApp, _server, _webApiConfig);

            _webApiConfig.EnsureInitialized();
        }

        public virtual void Dispose()
        {
            _webApiConfig?.Dispose();
            _server?.Dispose();
        }
    }
}