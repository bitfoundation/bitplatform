using Bit.Core.Models;
using Bit.OData.Contracts;
using Bit.Owin.Contracts;
using Bit.WebApi.Contracts;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.OData.Batch;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;
using System.Web.OData.Routing.Conventions;

namespace Bit.OData
{
    public class WebApiODataMiddlewareConfiguration : IOwinMiddlewareConfiguration, IDisposable
    {
        private HttpConfiguration _webApiConfig;
        private HttpServer _server;
        private ODataBatchHandler _odataBatchHandler;

        public virtual IEnumerable<IODataServiceBuilder> OdataServiceBuilders { get; set; }
        public virtual IEnumerable<IWebApiConfigurationCustomizer> WebApiConfgurationCustomizers { get; set; }
        public virtual System.Web.Http.Dependencies.IDependencyResolver WebApiDependencyResolver { get; set; }
        public virtual IODataModelBuilderProvider ODataModelBuilderProvider { get; set; }
        public virtual IWebApiOwinPipelineInjector WebApiOwinPipelineInjector { get; set; }
        public virtual IContainerBuilder ContainerBuilder { get; set; }

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

            WebApiConfgurationCustomizers.ToList()
                .ForEach(webApiConfigurationCustomizer =>
                {
                    webApiConfigurationCustomizer.CustomizeWebApiConfiguration(_webApiConfig);
                });

            _server = new HttpServer(_webApiConfig);

            _webApiConfig.UseCustomContainerBuilder(() => ContainerBuilder);

            foreach (IGrouping<string, IODataServiceBuilder> odataServiceBuilders in OdataServiceBuilders.GroupBy(mp => mp.GetODataRoute()))
            {
                ODataModelBuilder modelBuilder = ODataModelBuilderProvider.GetODataModelBuilder(_webApiConfig, containerName: $"{odataServiceBuilders.Key}Context", @namespace: odataServiceBuilders.Key);

                foreach (IODataServiceBuilder odataServiceBuilder in odataServiceBuilders)
                {
                    odataServiceBuilder.BuildModel(modelBuilder);
                }

                string routeName = $"{odataServiceBuilders.Key}-odata";

                _odataBatchHandler = new DefaultODataBatchHandler(_server);

                _odataBatchHandler.MessageQuotas.MaxOperationsPerChangeset = int.MaxValue;

                _odataBatchHandler.MessageQuotas.MaxPartsPerBatch = int.MaxValue;

                _odataBatchHandler.MessageQuotas.MaxNestingDepth = int.MaxValue;

                _odataBatchHandler.MessageQuotas.MaxReceivedMessageSize = long.MaxValue;

                _odataBatchHandler.ODataRouteName = routeName;

                IEnumerable<IODataRoutingConvention> conventions = ODataRoutingConventions.CreateDefault();

                IEdmModel edmModel = modelBuilder.GetEdmModel();

                _webApiConfig.MapODataServiceRoute(routeName, odataServiceBuilders.Key, builder =>
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
