using System;
using System.Web.Http;
using Bit.Api.Middlewares.WebApi.Contracts;
using Bit.Owin.Middlewares;
using NWebsec.Owin;
using Owin;

namespace Bit.Api.Middlewares.WebApi.Implementations
{
    public class DefaultWebApiOwinPipelineInjector : IWebApiOwinPipelineInjector
    {
        public virtual void UseWebApiOData(IAppBuilder owinApp, HttpServer server)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            if (server == null)
                throw new ArgumentNullException(nameof(server));

            owinApp.Map("/api", innerApp =>
            {
                innerApp.Use<AddAcceptCharsetToRequestHeadersIfNotAnyMiddleware>();
                innerApp.UseXContentTypeOptions();
                innerApp.UseWebApi(server);
            });
        }
    }
}
