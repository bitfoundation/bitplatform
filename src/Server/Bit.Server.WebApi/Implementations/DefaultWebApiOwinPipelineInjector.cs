using System;
using System.Web.Http;
using Bit.WebApi.Contracts;
using Owin;

namespace Bit.WebApi.Implementations
{
    public class DefaultWebApiOwinPipelineInjector : IWebApiOwinPipelineInjector
    {
        public virtual void UseWebApi(IAppBuilder owinApp, HttpServer server, HttpConfiguration webApiConfiguration)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            if (server == null)
                throw new ArgumentNullException(nameof(server));

            if (webApiConfiguration == null)
                throw new ArgumentNullException(nameof(webApiConfiguration));

            owinApp.Map("/api", innerApp =>
            {
                innerApp.UseXContentTypeOptions();
                innerApp.UseWebApi(server);
            });
        }
    }
}
