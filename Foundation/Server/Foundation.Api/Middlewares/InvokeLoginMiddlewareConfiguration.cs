using Foundation.Api.Contracts;
using System;
using Owin;

namespace Foundation.Api.Middlewares
{
    public class InvokeLoginMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            owinApp.Map("/InvokeLogin", innerApp =>
            {
                innerApp.Use<InvokeLoginMiddleware>();
            });
        }
    }
}
