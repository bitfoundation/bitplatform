using Bit.WebApi.Contracts;
using Owin;
using System;
using System.Web.Http;

namespace Bit.OData.Implementations
{
    public class DefaultWebApiODataOwinPipelineInjector : IWebApiOwinPipelineInjector
    {
        public virtual void UseWebApi(IAppBuilder owinApp, HttpServer server, HttpConfiguration webApiConfiguration)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            if (server == null)
                throw new ArgumentNullException(nameof(server));

            owinApp.Map("/odata", innerApp =>
            {
                innerApp.UseXContentTypeOptions();
                innerApp.UseWebApi(server);
            });
        }
    }
}
