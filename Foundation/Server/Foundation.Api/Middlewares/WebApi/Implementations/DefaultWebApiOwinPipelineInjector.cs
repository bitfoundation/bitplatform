using Foundation.Api.Middlewares.WebApi.OData.Contracts;
using Owin;
using System.Web.Http;
using NWebsec.Owin;
using System;
using Foundation.Api.Middlewares.WebApi.Contracts;

namespace Foundation.Api.Middlewares.WebApi.Implementations
{
    public class DefaultWebApiOwinPipelineInjector : IWebApiOwinPipelineInjector
    {
        public virtual void UseWebApiOData(IAppBuilder owinApp, HttpServer server)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            if (server == null)
                throw new ArgumentNullException(nameof(server));

            owinApp.Map($"/api", innerApp =>
            {
                innerApp.Use<OwinNoCacheResponseMiddleware>();
                innerApp.UseXContentTypeOptions();
                innerApp.UseWebApi(server);
            });
        }
    }
}
