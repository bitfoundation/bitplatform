using Foundation.Api.Contracts;
using System;
using Owin;

namespace Foundation.Api.Middlewares
{
    public class InvokeLogOutMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            owinApp.Map("/InvokeLogout", innerApp =>
            {
                innerApp.Use<InvokeLogOutMiddleware>();
            });
        }
    }
}
