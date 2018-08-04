using Bit.Owin.Contracts;
using Owin;
using System;

namespace Bit.Owin.Middlewares
{
    public class InvokeLogOutMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            owinApp.Map("/InvokeLogout", innerApp =>
            {
                innerApp.Use<OwinNoCacheResponseMiddleware>();
                innerApp.Use<InvokeLogOutMiddleware>();
            });
        }
    }
}
