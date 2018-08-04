using Bit.Owin.Contracts;
using Owin;
using System;

namespace Bit.Owin.Middlewares
{
    public class InvokeLoginMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            owinApp.Map("/InvokeLogin", innerApp =>
            {
                innerApp.Use<OwinNoCacheResponseMiddleware>();
                innerApp.Use<InvokeLoginMiddleware>();
            });
        }
    }
}
