using Bit.Core.Implementations;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Bit.WebApi.Contracts;
using Microsoft.Web.Http.Routing;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Routing;

namespace Bit.WebApi
{
    public class WebApiMiddlewareConfiguration : IOwinMiddlewareConfiguration, IDisposable
    {
        public virtual IEnumerable<IWebApiConfigurationCustomizer> WebApiConfigurationCustomizers { get; set; }
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

            _webApiConfig.Formatters.JsonFormatter.SerializerSettings.ContractResolver = DefaultJsonContentFormatter.SerializeSettings().ContractResolver;

            WebApiConfigurationCustomizers.ToList()
                .ForEach(webApiConfigurationCustomizer =>
                {
                    webApiConfigurationCustomizer.CustomizeWebApiConfiguration(_webApiConfig);
                });

            _server = new HttpServer(_webApiConfig);

            DefaultInlineConstraintResolver constraintResolver = new DefaultInlineConstraintResolver()
            {
                ConstraintMap = { ["apiVersion"] = typeof(ApiVersionRouteConstraint) }
            };

            _webApiConfig.AddApiVersioning(apiVerOptions =>
            {
                apiVerOptions.ReportApiVersions = true;
                apiVerOptions.AssumeDefaultVersionWhenUnspecified = true;
            });

            _webApiConfig.MapHttpAttributeRoutes(constraintResolver);

            if (_webApiConfig.Properties.TryGetValue("MultiVersionSwaggerConfiguration", out object actionObj))
            {
                ((Action)actionObj).Invoke();
            }

            _webApiConfig.Routes.MapHttpRoute(name: "default", routeTemplate: "{controller}/{action}", defaults: new { action = RouteParameter.Optional });

            owinApp.UseAutofacWebApi(_webApiConfig);

            WebApiOwinPipelineInjector.UseWebApi(owinApp, _server, _webApiConfig);

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
