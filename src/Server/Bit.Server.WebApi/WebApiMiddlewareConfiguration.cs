using Bit.Core.Implementations;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Bit.WebApi.Contracts;
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
        public virtual IEnumerable<IWebApiConfigurationCustomizer> WebApiConfigurationCustomizers { get; set; } = default!;
        public virtual System.Web.Http.Dependencies.IDependencyResolver WebApiDependencyResolver { get; set; } = default!;
        public virtual IWebApiOwinPipelineInjector WebApiOwinPipelineInjector { get; set; } = default!;
        public virtual AppEnvironment AppEnvironment { get; set; } = default!;
        public virtual WebApiHttpServerFactory WebApiHttpServerFactory { get; set; } = default!;
        public virtual WebApiInlineConstraintResolverFactory WebApiInlineConstraintResolverFactory { get; set; } = default!;

        private HttpConfiguration? _webApiConfig;
        private HttpServer? _server;

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            _webApiConfig = new HttpConfiguration();
            _webApiConfig.SuppressHostPrincipal();

            _webApiConfig.IncludeErrorDetailPolicy = AppEnvironment.DebugMode ? IncludeErrorDetailPolicy.LocalOnly : IncludeErrorDetailPolicy.Never;

            _webApiConfig.DependencyResolver = WebApiDependencyResolver;

            _webApiConfig.Formatters.JsonFormatter.SerializerSettings = DefaultJsonContentFormatter.SerializeSettings();

            WebApiConfigurationCustomizers.ToList()
                .ForEach(webApiConfigurationCustomizer =>
                {
                    webApiConfigurationCustomizer.CustomizeWebApiConfiguration(_webApiConfig);
                });

            _server = WebApiHttpServerFactory(_webApiConfig);

            IInlineConstraintResolver constraintResolver = WebApiInlineConstraintResolverFactory();

            _webApiConfig.AddApiVersioning(apiVerOptions =>
            {
                apiVerOptions.ReportApiVersions = true;
                apiVerOptions.AssumeDefaultVersionWhenUnspecified = true;
            });

            _webApiConfig.MapHttpAttributeRoutes(constraintResolver);

            if (_webApiConfig.Properties.TryGetValue("MultiVersionSwaggerConfiguration", out object? actionObj))
            {
                ((Action)actionObj).Invoke();
            }

            _webApiConfig.Routes.MapHttpRoute(name: "default", routeTemplate: "{controller}/{action}", defaults: new { action = RouteParameter.Optional });

            owinApp.UseAutofacWebApi(_webApiConfig);

            WebApiOwinPipelineInjector.UseWebApi(owinApp, _server, _webApiConfig);

            _webApiConfig.EnsureInitialized();
        }

        private bool isDisposed;

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;
            _webApiConfig?.Dispose();
            _server?.Dispose();
            isDisposed = true;
        }

        ~WebApiMiddlewareConfiguration()
        {
            Dispose(false);
        }
    }
}
