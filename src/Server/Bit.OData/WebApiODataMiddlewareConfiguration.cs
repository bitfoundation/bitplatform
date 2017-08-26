using Bit.Core.Contracts;
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
        private readonly AppEnvironment _activeAppEnvironment;
        private readonly IEnumerable<IODataServiceBuilder> _odataServiceBuilders;
        private readonly IEnumerable<IWebApiConfigurationCustomizer> _webApiConfgurationCustomizers;
        private readonly System.Web.Http.Dependencies.IDependencyResolver _webApiDependencyResolver;
        private readonly IODataModelBuilderProvider _oDataModelBuilderProvider;
        private HttpConfiguration _webApiConfig;
        private HttpServer _server;
        private ODataBatchHandler _odataBatchHandler;
        private readonly IWebApiOwinPipelineInjector _webApiOwinPipelineInjector;
        private readonly IContainerBuilder _containerBuilder;

#if DEBUG
        protected WebApiODataMiddlewareConfiguration()
        {
        }
#endif

        public WebApiODataMiddlewareConfiguration(IAppEnvironmentProvider appEnvironmentProvider,
            IEnumerable<IODataServiceBuilder> odataServiceBuilders, IEnumerable<IWebApiConfigurationCustomizer> webApiConfgurationCustomizers, System.Web.Http.Dependencies.IDependencyResolver webApiDependencyResolver, IODataModelBuilderProvider oDataModelBuilderProvider, IWebApiOwinPipelineInjector webApiOwinPipelineInjector, IContainerBuilder containerBuilder)
        {
            if (odataServiceBuilders == null)
                throw new ArgumentNullException(nameof(odataServiceBuilders));

            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            if (webApiConfgurationCustomizers == null)
                throw new ArgumentNullException(nameof(webApiConfgurationCustomizers));

            if (webApiDependencyResolver == null)
                throw new ArgumentNullException(nameof(webApiDependencyResolver));

            if (oDataModelBuilderProvider == null)
                throw new ArgumentNullException(nameof(oDataModelBuilderProvider));

            if (webApiOwinPipelineInjector == null)
                throw new ArgumentNullException(nameof(webApiOwinPipelineInjector));

            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));

            _activeAppEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();
            _odataServiceBuilders = odataServiceBuilders;
            _webApiConfgurationCustomizers = webApiConfgurationCustomizers;
            _webApiDependencyResolver = webApiDependencyResolver;
            _oDataModelBuilderProvider = oDataModelBuilderProvider;
            _webApiOwinPipelineInjector = webApiOwinPipelineInjector;
            _containerBuilder = containerBuilder;
        }

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            _webApiConfig = new HttpConfiguration();
            _webApiConfig.SuppressHostPrincipal();

            _webApiConfig.SetTimeZoneInfo(TimeZoneInfo.Utc);

            _webApiConfig.Formatters.Clear();

            _webApiConfig.IncludeErrorDetailPolicy = _activeAppEnvironment.DebugMode ? IncludeErrorDetailPolicy.LocalOnly : IncludeErrorDetailPolicy.Never;

            _webApiConfig.DependencyResolver = _webApiDependencyResolver;

            _webApiConfgurationCustomizers.ToList()
                .ForEach(webApiConfigurationCustomizer =>
                {
                    webApiConfigurationCustomizer.CustomizeWebApiConfiguration(_webApiConfig);
                });

            _server = new HttpServer(_webApiConfig);

            _webApiConfig.UseCustomContainerBuilder(() => _containerBuilder);

            foreach (IGrouping<string, IODataServiceBuilder> odataServiceBuilders in _odataServiceBuilders.GroupBy(mp => mp.GetODataRoute()))
            {
                ODataModelBuilder modelBuilder = _oDataModelBuilderProvider.GetODataModelBuilder(_webApiConfig, containerName: $"{odataServiceBuilders.Key}Context", @namespace: odataServiceBuilders.Key);

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
                    builder.AddService(ServiceLifetime.Singleton, sp => _webApiDependencyResolver);
                });
            }

            owinApp.UseAutofacWebApi(_webApiConfig);

            _webApiOwinPipelineInjector.UseWebApiOData(owinApp, _server, _webApiConfig);

            _webApiConfig.EnsureInitialized();
        }

        public virtual void Dispose()
        {
            _odataBatchHandler?.Dispose();
            _webApiConfig?.Dispose();
            _server?.Dispose();
        }
    }
}
