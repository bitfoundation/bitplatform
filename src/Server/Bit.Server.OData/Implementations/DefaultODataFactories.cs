using Bit.Core.Contracts;
using Bit.OData.Contracts;
using Microsoft.AspNet.OData.Batch;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Web.Http;

namespace Bit.OData.Implementations
{
    public static class DefaultODataFactories
    {
        public static IDependencyManager RegisterODataFactories(this IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.RegisterUsing(resolver => new ODataBatchHandlerHandlerFactory((httpServer, oDataRouteName) => ODataBatchHandlerHandlerFactory<DefaultODataBatchHandler>(resolver, httpServer, oDataRouteName)), lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);
            dependencyManager.RegisterUsing(resolver => new ODataHttpControllerSelectorFactory((webApiConfiguration) => HttpControllerSelectorFactory<DefaultODataHttpControllerSelector>(resolver, webApiConfiguration)), lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);
            dependencyManager.RegisterUsing(resolver => new ODataHttpServerFactory((webApiConfiguration) => ODataHttpServerFactory<HttpServer>(resolver, webApiConfiguration)), lifeCycle: DependencyLifeCycle.Transient, overwriteExisting: false);

            return dependencyManager;
        }

        public static TODataBatchHandler ODataBatchHandlerHandlerFactory<TODataBatchHandler>(IDependencyResolver resolver, HttpServer httpServer, string oDataRouteName)
            where TODataBatchHandler : DefaultODataBatchHandler
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            if (httpServer == null)
                throw new ArgumentNullException(nameof(httpServer));

            if (oDataRouteName == null)
                throw new ArgumentNullException(nameof(oDataRouteName));

            var odataBatchHandler = ActivatorUtilities.CreateInstance<TODataBatchHandler>(resolver.Resolve<IServiceProvider>(), new object[] { httpServer });

            odataBatchHandler.MessageQuotas.MaxOperationsPerChangeset = int.MaxValue;

            odataBatchHandler.MessageQuotas.MaxPartsPerBatch = int.MaxValue;

            odataBatchHandler.MessageQuotas.MaxNestingDepth = int.MaxValue;

            odataBatchHandler.MessageQuotas.MaxReceivedMessageSize = long.MaxValue;

            odataBatchHandler.ODataRouteName = oDataRouteName;

            return odataBatchHandler;
        }

        public static THttpControllerSelector HttpControllerSelectorFactory<THttpControllerSelector>(IDependencyResolver resolver, HttpConfiguration webApiConfiguration)
            where THttpControllerSelector : DefaultODataHttpControllerSelector
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            if (webApiConfiguration == null)
                throw new ArgumentNullException(nameof(webApiConfiguration));

            return ActivatorUtilities.CreateInstance<THttpControllerSelector>(resolver.Resolve<IServiceProvider>(), new object[] { webApiConfiguration });
        }

        public static THttpServer ODataHttpServerFactory<THttpServer>(IDependencyResolver resolver, HttpConfiguration webApiConfiguration)
            where THttpServer : HttpServer
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            if (webApiConfiguration == null)
                throw new ArgumentNullException(nameof(webApiConfiguration));

            return ActivatorUtilities.CreateInstance<THttpServer>(resolver.Resolve<IServiceProvider>(), new object[] { webApiConfiguration });
        }
    }
}
