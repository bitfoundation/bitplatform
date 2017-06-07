using System;
using Bit.Owin.Contracts;
using Owin;

namespace Bit.Owin.Middlewares
{
    public class ReadAuthTokenFromCookieMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            owinApp.Use<ReadAuthTokenFromCookieMiddleware>();
        }
    }
}