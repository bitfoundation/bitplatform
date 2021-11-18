using Bit.Core.Models;
using Bit.OData.Contracts;
using Bit.Owin.Contracts;
using Bit.WebApi.Contracts;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Dispatcher;

[assembly: ODataModule("Bit")]

namespace Bit.OData
{
    public class WebApiODataMiddlewareConfiguration : IOwinMiddlewareConfiguration, IDisposable
    {
        private HttpConfiguration? _webApiConfig;
        private HttpServer? _server;
        private ODataBatchHandler? _odataBatchHandler;

        public virtual IODataModuleConfiguration ODataModuleConfiguration { get; set; } = default!;
        public virtual IEnumerable<IWebApiConfigurationCustomizer> WebApiConfigurationCustomizers { get; set; } = default!;
        public virtual System.Web.Http.Dependencies.IDependencyResolver WebApiDependencyResolver { get; set; } = default!;
        public virtual IODataModelBuilderProvider ODataModelBuilderProvider { get; set; } = default!;
        public virtual IWebApiOwinPipelineInjector WebApiOwinPipelineInjector { get; set; } = default!;
        public virtual IApiAssembliesProvider ApiAssembliesProvider { get; set; } = default!;
        public virtual ODataBatchHandlerHandlerFactory ODataBatchHandlerHandlerFactory { get; set; } = default!;
        public virtual ODataHttpControllerSelectorFactory ODataHttpControllerSelectorFactory { get; set; } = default!;
        public virtual ODataHttpServerFactory ODataHttpServerFactory { get; set; } = default!;
        public virtual AppEnvironment AppEnvironment { get; set; } = default!;

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            _webApiConfig = new HttpConfiguration();
            _webApiConfig.SuppressHostPrincipal();

            _webApiConfig.SetTimeZoneInfo(TimeZoneInfo.Utc);

            _webApiConfig.Formatters.Clear();

            _webApiConfig.IncludeErrorDetailPolicy = AppEnvironment.DebugMode ? IncludeErrorDetailPolicy.LocalOnly : IncludeErrorDetailPolicy.Never;

            _webApiConfig.DependencyResolver = WebApiDependencyResolver;

            _webApiConfig.Services.Replace(typeof(IHttpControllerSelector), ODataHttpControllerSelectorFactory(_webApiConfig));

            WebApiConfigurationCustomizers.ToList()
                .ForEach(webApiConfigurationCustomizer =>
                {
                    webApiConfigurationCustomizer.CustomizeWebApiConfiguration(_webApiConfig);
                });

            _server = ODataHttpServerFactory(_webApiConfig);

            _webApiConfig.UseCustomContainerBuilder(() => (IContainerBuilder)WebApiDependencyResolver.GetService(typeof(IContainerBuilder)));

            var odataModulesAndAssembliesGroups = ApiAssembliesProvider.GetApiAssemblies()
                .SelectMany(asm =>
                {
                    ODataModuleAttribute[] odataModuleAttributes = asm.GetCustomAttributes<ODataModuleAttribute>().ToArray();

                    return odataModuleAttributes.Select(oma => new { ODataModule = oma, Assembly = asm });
                })
                .GroupBy(odataModuleAndAssembly => odataModuleAndAssembly.ODataModule.ODataRouteName);

            foreach (var odataModuleAndAssemblyGroup in odataModulesAndAssembliesGroups)
            {
                ODataModelBuilder modelBuilder = ODataModelBuilderProvider.GetODataModelBuilder(_webApiConfig, containerName: $"{odataModuleAndAssemblyGroup.Key}Context", @namespace: null);

                foreach (var odataModuleAndAssembly in odataModuleAndAssemblyGroup)
                {
                    ODataModuleConfiguration.ConfigureODataModule(odataModuleAndAssemblyGroup.Key, odataModuleAndAssembly.Assembly, modelBuilder);
                }

                string routeName = $"{odataModuleAndAssemblyGroup.Key}-odata";

                _odataBatchHandler = ODataBatchHandlerHandlerFactory(_server, routeName);

                IEdmModel edmModel = modelBuilder.GetEdmModel();

                _webApiConfig.MapODataServiceRoute(routeName, odataModuleAndAssemblyGroup.Key, builder =>
                {
                    builder.AddService(ServiceLifetime.Singleton, sp => edmModel);
                    builder.AddService(ServiceLifetime.Singleton, sp => _odataBatchHandler);
                });
            }

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
            _odataBatchHandler?.Dispose();
            _webApiConfig?.Dispose();
            _server?.Dispose();
            isDisposed = true;
        }

        ~WebApiODataMiddlewareConfiguration()
        {
            Dispose(false);
        }
    }
}
