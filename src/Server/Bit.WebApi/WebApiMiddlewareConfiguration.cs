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
        public virtual IEnumerable<IWebApiConfigurationCustomizer> WebApiConfgurationCustomizers { get; set; }
        public virtual System.Web.Http.Dependencies.IDependencyResolver WebApiDependencyResolver { get; set; }
        public virtual IWebApiOwinPipelineInjector WebApiOwinPipelineInjector { get; set; }

        public virtual AppEnvironment AppEnvironment { get; set; }

        private HttpConfiguration _webApiConfig;
        private HttpServer _server;

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            _webApiConfig = new HttpConfiguration();
            _webApiConfig.SuppressHostPrincipal();

            _webApiConfig.IncludeErrorDetailPolicy = AppEnvironment.DebugMode ? IncludeErrorDetailPolicy.LocalOnly : IncludeErrorDetailPolicy.Never;

            _webApiConfig.DependencyResolver = WebApiDependencyResolver;

            WebApiConfgurationCustomizers.ToList()
                .ForEach(webApiConfigurationCustomizer =>
                {
                    webApiConfigurationCustomizer.CustomizeWebApiConfiguration(_webApiConfig);
                });

            _server = new HttpServer(_webApiConfig);

            _webApiConfig.MapHttpAttributeRoutes();

            _webApiConfig.Routes.MapHttpRoute(name: "default", routeTemplate: "{controller}/{action}", defaults: new { action = RouteParameter.Optional });

            owinApp.UseAutofacWebApi(_webApiConfig);

            WebApiOwinPipelineInjector.UseWebApiOData(owinApp, _server, _webApiConfig);

            _webApiConfig.EnsureInitialized();
        }

        public virtual void Dispose()
        {
            _webApiConfig?.Dispose();
            _server?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}