using Owin;
using System.Web.Http;
using NWebsec.Owin;
using System;
using Foundation.Api.Middlewares.WebApi.Contracts;

namespace Foundation.Api.Middlewares.WebApi.OData.Implementations
{
    public class DefaultWebApiODataOwinPipelineInjector : IWebApiOwinPipelineInjector
    {
        public virtual void UseWebApiOData(IAppBuilder owinApp, HttpServer server)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            if (server == null)
                throw new ArgumentNullException(nameof(server));

            owinApp.Map($"/odata", innerApp =>
            {
                innerApp.Use<AddAcceptCharsetToRequestHeadersIfNotAnyMiddleware>();
                innerApp.UseXContentTypeOptions();
                innerApp.UseWebApi(server);
            });
        }
    }
}
