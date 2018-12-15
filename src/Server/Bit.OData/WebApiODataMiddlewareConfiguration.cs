using Bit.Core.Models;
using Bit.OData.Contracts;
using Bit.OData.Implementations;
using Bit.Owin.Contracts;
using Bit.WebApi.Contracts;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Routing.Conventions;
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
        private HttpConfiguration _webApiConfig;
        private HttpServer _server;
        private ODataBatchHandler _odataBatchHandler;

        public virtual IODataModuleConfiguration ODataModuleConfiguration { get; set; }
        public virtual IEnumerable<IWebApiConfigurationCustomizer> WebApiConfigurationCustomizers { get; set; }
        public virtual System.Web.Http.Dependencies.IDependencyResolver WebApiDependencyResolver { get; set; }
        public virtual IODataModelBuilderProvider ODataModelBuilderProvider { get; set; }
        public virtual IWebApiOwinPipelineInjector WebApiOwinPipelineInjector { get; set; }
        public virtual IContainerBuilder ContainerBuilder { get; set; }

        public virtual IApiAssembliesProvider ApiAssembliesProvider { get; set; }

        public virtual AppEnvironment AppEnvironment { get; set; }

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

            _webApiConfig.Services.Replace(typeof(IHttpControllerSelector), new DefaultODataHttpControllerSelector(_webApiConfig));

            WebApiConfigurationCustomizers.ToList()
                .ForEach(webApiConfigurationCustomizer =>
                {
                    webApiConfigurationCustomizer.CustomizeWebApiConfiguration(_webApiConfig);
                });

            _server = new HttpServer(_webApiConfig);

            _webApiConfig.UseCustomContainerBuilder(() => ContainerBuilder);

            var odataModulesAndAssembliesGroups = ApiAssembliesProvider.GetApiAssemblies()
                .Select(asm =>
                {
                    ODataModuleAttribute odataModuleAttribute = asm.GetCustomAttribute<ODataModuleAttribute>();

                    return new { ODataModule = odataModuleAttribute, Assembly = asm };
                })
                .Where(odataModuleAndAssembly => odataModuleAndAssembly.ODataModule != null)
                .GroupBy(odataModuleAndAssembly => odataModuleAndAssembly.ODataModule.ODataRouteName);

            foreach (var odataModuleAndAssemblyGroup in odataModulesAndAssembliesGroups)
            {
                ODataModelBuilder modelBuilder = ODataModelBuilderProvider.GetODataModelBuilder(_webApiConfig, containerName: $"{odataModuleAndAssemblyGroup.Key}Context", @namespace: null);

                foreach (var odataModuleAndAssembly in odataModuleAndAssemblyGroup)
                {
                    ODataModuleConfiguration.ConfigureODataModule(odataModuleAndAssemblyGroup.Key, odataModuleAndAssembly.Assembly, modelBuilder);
                }

                string routeName = $"{odataModuleAndAssemblyGroup.Key}-odata";

                _odataBatchHandler = new DefaultODataBatchHandler(_server);

                _odataBatchHandler.MessageQuotas.MaxOperationsPerChangeset = int.MaxValue;

                _odataBatchHandler.MessageQuotas.MaxPartsPerBatch = int.MaxValue;

                _odataBatchHandler.MessageQuotas.MaxNestingDepth = int.MaxValue;

                _odataBatchHandler.MessageQuotas.MaxReceivedMessageSize = long.MaxValue;

                _odataBatchHandler.ODataRouteName = routeName;

                IEnumerable<IODataRoutingConvention> conventions = ODataRoutingConventions.CreateDefault();

                IEdmModel edmModel = modelBuilder.GetEdmModel();

                _webApiConfig.MapODataServiceRoute(routeName, odataModuleAndAssemblyGroup.Key, builder =>
                {
                    builder.AddService(ServiceLifetime.Singleton, sp => conventions);
                    builder.AddService(ServiceLifetime.Singleton, sp => edmModel);
                    builder.AddService(ServiceLifetime.Singleton, sp => _odataBatchHandler);
                    builder.AddService(ServiceLifetime.Singleton, sp => WebApiDependencyResolver);
                });
            }

            owinApp.UseAutofacWebApi(_webApiConfig);

            WebApiOwinPipelineInjector.UseWebApi(owinApp, _server, _webApiConfig);

            _webApiConfig.EnsureInitialized();
        }

        public virtual void Dispose()
        {
            _odataBatchHandler?.Dispose();
            _webApiConfig?.Dispose();
            _server?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
