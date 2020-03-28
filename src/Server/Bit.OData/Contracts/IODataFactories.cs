using Microsoft.AspNet.OData.Batch;
using System.Web.Http;
using System.Web.Http.Dispatcher;

namespace Bit.OData.Contracts
{
    public delegate ODataBatchHandler ODataBatchHandlerHandlerFactory(HttpServer httpServer, string oDataRouteName);
    public delegate IHttpControllerSelector ODataHttpControllerSelectorFactory(HttpConfiguration webApiConfiguration);
    public delegate HttpServer ODataHttpServerFactory(HttpConfiguration webApiConfiguration);
    // You can customie OData HttpConfiguration (webApiConfiguration) using IWebApiConfigurationCustomizer
}
