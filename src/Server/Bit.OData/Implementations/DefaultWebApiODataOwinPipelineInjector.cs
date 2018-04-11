using System;
using System.Web.Http;
using Bit.WebApi.Contracts;
using Owin;

namespace Bit.OData.Implementations
{
    public class DefaultWebApiODataOwinPipelineInjector : IWebApiOwinPipelineInjector
    {
        public virtual void UseWebApiOData(IAppBuilder owinApp, HttpServer server, HttpConfiguration webApiConfiguration)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            if (server == null)
                throw new ArgumentNullException(nameof(server));

            webApiConfiguration.Properties.TryAdd("Owin-Branch-Route-Prefix", "odata");

            owinApp.Map("/odata", innerApp =>
            {
                innerApp.UseXContentTypeOptions();
                innerApp.UseWebApi(server);
            });
        }
    }
}
